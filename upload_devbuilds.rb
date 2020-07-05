#!/usr/bin/env ruby
# frozen_string_literal: true

# This scripts uploads devbuilds that were written to 'builds' folder by make_release script
require 'optparse'
require 'sha3'

require_relative 'bootstrap_rubysetupsystem'
require_relative 'RubySetupSystem/RubyCommon'

@options = {
  custom_targets: false,
  targets: ALL_TARGETS,
  retries: 2,
  zip: true
}

OptionParser.new do |opts|
  opts.banner = "Usage: #{$PROGRAM_NAME} [options]"

  opts.on('-t', '--targets target1,target2', Array,
          'Export targets to use. Default is all') do |targets|
    @options[:custom_targets] = true
    @options[:targets] = targets
  end
  opts.on('-r', '--retries count', Integer,
          'How many export retries to do to avoid spurious failures') do |r|
    @options[:retries] = r
  end
  opts.on('-z', '--[no-]zip', 'Disables packaging the exported game') do |b|
    @options[:zip] = b
  end
  opts.on('-d', '--[no-]dehydrated', 'Makes dehydrated devbuilds') do |b|
    @options[:dehydrate] = b
    @options[:zip] = !b
  end
end.parse!

onError "Unhandled parameters: #{ARGV}" unless ARGV.empty?

def git_branch
  `git rev-parse --symbolic-full-name --abbrev-ref HEAD`.strip
end

puts 'TODO: implement'
exit 1
