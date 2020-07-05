# frozen_string_literal: true

# Common upload functions
# This is a separate file from the upload script to leave open the
# option of the release build making script starting the upload while
# another build is being created

DEFAULT_PARALLEL_UPLOADS = 5

DEVCENTER_URL = 'https://dev.revolutionarygamesstudio.com'

def git_branch
  `git rev-parse --symbolic-full-name --abbrev-ref HEAD`.strip
end

# Manages uploading dehydrated devbuilds and the object cache entries missing from the server
class DevBuildUploader
  def initialize(folder, cache, options)
    @folder = folder
    @cache = cache
    @parallel = options[:parallel_upload]
    @base_url = options[:url]
    @retries = options[:retries]

    @access_key = ENV.fetch('THRIVE_DEVCENTER_ACCESS_KEY', nil)

    puts 'Uploading anonymous devbuilds' unless @access_key
  end

  # Run the upload operation
  def run; end
end
