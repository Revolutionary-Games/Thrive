#!/usr/bin/env ruby
# frozen_string_literal: true

require 'os'

require_relative 'steam_version_helpers'

# Enable for the native platform
if OS.windows?
  enable_steam_build 'Windows Desktop'
else
  # OSX uses the same platform library so we don't need an explicit check here
  enable_steam_build 'Linux/X11'
end
