# Library part of the analysis toggle
require 'json'

require_relative '../../RubySetupSystem/RubyCommon'

MAIN_FOLDER = File.expand_path File.join(__dir__, '../../')

MODE_CACHE_PATH = File.join(MAIN_FOLDER, 'fast_build_mode.json')
DEFAULT_SETTINGS = { analysis_mode: true }.freeze

BUILD_PROPS = File.join MAIN_FOLDER, 'Directory.Build.props'

NORMAL_BUILD = File.join(__dir__, 'OriginalBuild.xml')
FAST_BUILD = File.join(__dir__, 'AnalyzerLessBuild.xml')

def load_analysis_settings
  return DEFAULT_SETTINGS.dup unless File.exist? MODE_CACHE_PATH

  JSON.parse File.read(MODE_CACHE_PATH), symbolize_names: true
end

def save_analysis_settings(settings)
  File.write(MODE_CACHE_PATH, JSON.dump(settings))
end

def write_new_build_props(source)
  contents = File.read source

  File.write BUILD_PROPS, contents
  puts "Wrote new build props to: #{BUILD_PROPS}"
end

def change_analysis_mode(target_mode, current_settings)
  if target_mode == true
    info 'Going to analysis mode'
    write_new_build_props NORMAL_BUILD
  else
    info 'Going to fast build (no analysis) mode'
    write_new_build_props FAST_BUILD
  end

  Dir.chdir(MAIN_FOLDER) do
    runOpen3Checked 'nuget', 'restore'
  end

  current_settings[:analysis_mode] = target_mode
  save_analysis_settings current_settings
end

# Changes to the wanted mode if needed
def perform_analysis_mode_check(wanted_mode, quiet: false)
  current_settings = load_analysis_settings

  if current_settings[:analysis_mode] == wanted_mode
    info 'Already in right analysis mode' unless quiet
  else
    info "Changing analysis mode to: #{wanted_mode}" unless quiet
    change_analysis_mode wanted_mode, current_settings
  end
end
