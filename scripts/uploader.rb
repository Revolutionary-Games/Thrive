# frozen_string_literal: true

require 'json'
require 'httparty'
require 'parallel'
require 'sha3'

require_relative 'dehydrate'

# Common upload functions
# This is a separate file from the upload script to leave open the
# option of the release build making script starting the upload while
# another build is being created

DEFAULT_PARALLEL_UPLOADS = 5

DEVCENTER_URL = 'https://dev.revolutionarygamesstudio.com'

MAX_SERVER_BATCH_SIZE = 100

# Manages uploading dehydrated devbuilds and the object cache entries missing from the server
class DevBuildUploader
  def initialize(folder, cache, options)
    @folder = folder
    @cache = cache
    @parallel = options[:parallel_upload]
    @base_url = options[:url]
    @retries = options[:retries]
    @delete = options[:delete_after_upload]

    @dehydrated_to_upload = []
    @devbuilds_to_upload = []
    @already_uploaded_to_delete = []

    @access_key = ENV.fetch('THRIVE_DEVCENTER_ACCESS_KEY', nil)

    puts 'Uploading anonymous devbuilds' unless @access_key
  end

  def headers
    if @access_key
      { 'X-Access-Code' => @access_key, 'Content-Type' => 'application/json' }
    else
      { 'Content-Type' => 'application/json' }
    end
  end

  # Run the upload operation
  def run
    info 'Starting devbuild upload'

    Dir.glob(File.join(@folder, '*.meta.json')) do |file|
      archive_file = file.chomp '.meta.json'
      check_build_for_upload file, archive_file
    end

    @dehydrated_to_upload.uniq!

    info "Beginning upload of #{@devbuilds_to_upload.size} devbuilds with "\
         "#{@dehydrated_to_upload.size} dehydrated objects"

    perform_uploads
    success 'DevBuild upload finished.'
    delete_already_existing
  end

  private

  def perform_uploads
    info 'Fetching tokens'
    things_to_upload = fetch_upload_tokens

    info 'Uploading dehydrated objects'
    Parallel.map(things_to_upload, in_threads: @parallel) do |obj|
      upload(*obj)
    end

    info 'Fetching tokens'
    things_to_upload = fetch_devbuild_upload_tokens

    info 'Uploading devbuilds'
    Parallel.map(things_to_upload, in_threads: @parallel) do |obj|
      upload(*obj[0..2], delete: @delete, meta: obj[3])
    end

    success 'Done uploading'
  end

  def fetch_upload_tokens
    # TODO: if the internet is slow it might not be a good idea to
    # fetch all of the tokens at once
    result = []

    @dehydrated_to_upload.each_slice(MAX_SERVER_BATCH_SIZE) do |group|
      data = with_retry do
        HTTParty.post(URI.join(@base_url, '/api/v1/devbuild/upload_objects'),
                      headers: headers, body: {
                        objects: group.map do |i|
                          { sha3: i, size: object_size(i) }
                        end
                      }.to_json)
      end

      data['upload'].each do |upload|
        result.append [path_from_hash(upload['sha3']), upload['upload_url'],
                       upload['verify_token']]
      end
    end

    result
  end

  def fetch_devbuild_upload_tokens
    result = []

    @devbuilds_to_upload.each do |build|
      data = with_retry do
        HTTParty.post(URI.join(@base_url, '/api/v1/devbuild/upload_devbuild'),
                      headers: headers, body: {
                        build_hash: build[:version],
                        build_branch: build[:branch],
                        build_platform: build[:platform],
                        build_size: File.size(build[:file]),
                        required_objects: build[:dehydrated_objects],
                        build_zip_hash: build[:build_zip_hash]
                      }.to_json)
      end

      onError "failed to receive upload url, response: #{data}" unless data['upload_url']

      result.append [build[:file], data['upload_url'],
                     data['verify_token'], build[:meta_file]]
    end

    result
  end

  def check_build_for_upload(file, archive_file)
    meta = JSON.parse File.read(file)

    begin
      version = meta['version']
      platform = meta['platform']
      branch = meta['branch']
      dehydrated_objects = meta['dehydrated']['objects']

      raise 'no version in file' unless version
    rescue StandardError
      onError 'Invalid devbuild meta content'
    end

    puts "Found devbuild: #{version}, #{platform}, #{branch}"

    data = with_retry do
      HTTParty.post(URI.join(@base_url, '/api/v1/devbuild/offer_devbuild'),
                    headers: headers, body: {
                      build_hash: version,
                      build_platform: platform
                    }.to_json)
    end

    unless data['upload']
      @already_uploaded_to_delete.append [archive_file, file] if @delete
      return
    end

    puts "Server doesn't have it."
    @devbuilds_to_upload.append({ file: archive_file, version: version, platform: platform,
                                  branch: branch, dehydrated_objects: dehydrated_objects,
                                  meta_file: file, build_zip_hash:
                                    SHA3::Digest::SHA256.file(archive_file).hexdigest })

    # Determine related objects to upload
    # MAX_SERVER_BATCH_SIZE
    dehydrated_objects.each_slice(MAX_SERVER_BATCH_SIZE) do |group|
      data = with_retry do
        HTTParty.post(URI.join(@base_url, '/api/v1/devbuild/offer_objects'),
                      headers: headers, body: {
                        objects: group.map do |i|
                                   { sha3: i, size: object_size(i) }
                                 end
                      }.to_json)
      end

      data['upload'].each do |upload|
        @dehydrated_to_upload.append upload
      end
    end
  end

  def path_from_hash(hash)
    File.join(@cache, "#{hash}.gz")
  end

  def object_size(hash)
    File.size path_from_hash(hash)
  end

  # Does the whole upload process
  def upload(file, url, token, delete: false, meta: nil)
    file_size = (File.size(file).to_f / 2**20).round(2)
    puts "Uploading file #{file} " \
         "with size of #{file_size} MiB"
    put_file file, url

    # Tell the server about upload success
    with_retry do
      HTTParty.post(URI.join(@base_url, '/api/v1/devbuild/finish'),
                    headers: headers, body: {
                      token: token
                    }.to_json)
    end

    return unless delete

    puts "Deleting successfully uploaded file: #{file}"
    File.unlink file
    File.unlink meta if meta
  end

  # Puts file to storage URL
  def put_file(file, url)
    with_retry do
      HTTParty.put(url, headers: { 'Content-Length' => File.size(file).to_s },
                        body_stream: File.open(file, 'rb'))
    end
  end

  def with_retry(needed_response_code: 200)
    time_to_wait = 20
    (1..@retries).each do |i|
      begin
        response = yield

        if response.code == 503
          puts "Error 503: waiting #{time_to_wait} seconds..."
          sleep(time_to_wait)
          time_to_wait *= 2
        elsif response.code != needed_response_code
          puts "Response: #{response}"
          raise "unexpected response code: #{response.code}"
        end

        return response
      rescue StandardError => e
        puts "HTTP request failed: #{e}, " +
             (i < @retries ? "retry attempt #{i}" : 'ran out of retries')
        sleep 1
      end
    end

    raise 'HTTP request ran out of retries'
  end

  def delete_already_existing
    @already_uploaded_to_delete.uniq.each do |file, meta|
      puts "Deleting build that server already had: #{file}"
      begin
        File.unlink file
      rescue StandardError => e
        error "Failed to delete file: #{e}"
      end
      begin
        File.unlink meta
      rescue StandardError => e
        error "Failed to delete file: #{e}"
      end
    end
  end
end
