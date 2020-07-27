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

    @dehydrated_to_upload = []
    @devbuilds_to_upload = []

    @access_key = ENV.fetch('THRIVE_DEVCENTER_ACCESS_KEY', nil)

    puts 'Uploading anonymous devbuilds' unless @access_key
  end

  def headers
    if @access_key
      { 'X-Access-Code' => @access_key }
    else
      {}
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
      upload(*obj)
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
                      })
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
                      })
      end

      onError "failed to receive upload url, response: #{data}" unless data['upload_url']

      result.append [build[:file], data['upload_url'],
                     data['verify_token']]
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
                    })
    end

    return unless data['upload']

    puts "Server doesn't have it."
    @devbuilds_to_upload.append({ file: archive_file, version: version, platform: platform,
                                  branch: branch, dehydrated_objects: dehydrated_objects,
                                  build_zip_hash:
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
                      })
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
  def upload(file, url, token)
    puts "Uploading file #{file}"
    put_file file, url

    # Tell the server about upload success
    with_retry do
      HTTParty.post(URI.join(@base_url, '/api/v1/devbuild/finish'),
                    headers: headers, body: {
                      token: token
                    })
    end
  end

  # Puts file to storage URL
  def put_file(file, url)
    with_retry do
      HTTParty.put(url, headers: { 'Content-Length' => File.size(file).to_s },
                        body_stream: File.open(file, 'rb'))
    end
  end

  def with_retry(needed_response_code: 200)
    (1..@retries).each do |i|
      begin
        response = yield

        if response.code != needed_response_code
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
end
