#!/usr/bin/env ruby
# frozen_string_literal: true

# This scripts uploads devbuilds that were written to 'builds' folder by make_release script
require 'optparse'
require 'sha3'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'
require_relative 'scripts/uploader'
require_relative 'scripts/dehydrate'

@options = {
  parallel_upload: DEFAULT_PARALLEL_UPLOADS,
  url: DEVCENTER_URL,
  retries: 3,
  delete_after_upload: true
}

OptionParser.new do |opts|
  opts.banner = "Usage: #{$PROGRAM_NAME} [options]"

  opts.on('-r', '--retries count', Integer,
          'How many upload export retries to do to avoid spurious failures') do |r|
    @options[:retries] = r
  end
  opts.on('--url devcenterurl', 'Custom URL to upload to') do |url|
    @options[:url] = url
  end
  opts.on('-j', '--parallel count', Integer, 'How many parallel uploads to do') do |p|
    @options[:parallel_upload] = p
  end
  opts.on('--[no-]delete-after-upload',
          'If specified dehydrated builds are deleted after upload') do |d|
    @options[:delete_after_upload] = d
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

DevBuildUploader.new(DEVBUILDS_FOLDER, DEHYDRATE_CACHE, @options).run
