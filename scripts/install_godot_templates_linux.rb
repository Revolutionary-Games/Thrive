#!/usr/bin/env ruby
# frozen_string_literal: true

# This script downloads and installs godot export templates for current version on Linux
require_relative 'templates_common'

run_template_download "https://downloads.tuxfamily.org/godotengine/#{GODOT_VERSION}/" \
                      "mono/Godot_v#{GODOT_VERSION}-stable_mono_export_templates.tpz",
                      'godot_mono_templates.tpz'
