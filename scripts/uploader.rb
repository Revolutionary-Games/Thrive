# frozen_string_literal: true

require 'json'
require 'httparty'

require_relative 'dehydrate'

# Common upload functions
# This is a separate file from the upload script to leave open the
# option of the release build making script starting the upload while
# another build is being created

DEFAULT_PARALLEL_UPLOADS = 5

DEVCENTER_URL = 'https://dev.revolutionarygamesstudio.com'

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

  # Run the upload operation
  def run
    info 'Starting devbuild upload'

    Dir.glob(File.join(@folder, '*.meta.json')) do |file|
      meta_name = File.basename file
      name = meta_name.chomp '.meta.json'

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
    end

    info "Beginning upload of #{@devbuilds_to_upload.size} devbuilds with "\
         "#{@dehydrated_to_upload.size} dehydrated objects"

    success 'DevBuild upload finished.'
  end
end
