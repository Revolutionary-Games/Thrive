#!/usr/bin/env ruby
# frozen_string_literal: true

# This script downloads and installs godot export templates needed for Steam exports on Linux
require_relative 'templates_common'

# GodotSteam releases can be found from: https://github.com/Gramps/GodotSteam/releases

run_template_download 'https://github.com/Gramps/GodotSteam/releases/download/'\
                      'g334-s152-gs3102/godotsteam-334-templates.tpz',
                      'godotsteam-334-templates.tpz'
