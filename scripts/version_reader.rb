# frozen_string_literal: true

ASSEMBLY_VERSION = /AssemblyVersion\("([\d.]+)"\)/.freeze
INFORMATIONAL_VERSION = /AssemblyInformationalVersion\("([^"]*)"\)/.freeze

THRIVE_VERSION_FILE = File.expand_path('../Properties/AssemblyInfo.cs', File.dirname(__FILE__))

# Reads thrive version from the code
def find_thrive_version
  version = nil
  additional_version = nil

  File.open(THRIVE_VERSION_FILE, 'r') do |f|
    f.each_line do |line|
      line.match(ASSEMBLY_VERSION) do |match|
        version = match[1]
      end
      line.match(INFORMATIONAL_VERSION) do |match|
        additional_version = match[1]
      end
    end
  end

  raise 'Failed to find AssemblyVersion for thrive' unless version

  "#{version}#{additional_version}"
end
