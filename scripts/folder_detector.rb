# frozen_string_literal: true

TOP_LEVEL_FILE_FOR_FOLDER_DETECT = 'Thrive.sln'

def detect_thrive_folder
  base_path = ''
  unless File.exist? TOP_LEVEL_FILE_FOR_FOLDER_DETECT
    base_path = '../'
    unless File.exist? "#{base_path}#{TOP_LEVEL_FILE_FOR_FOLDER_DETECT}"
      puts 'Failed to find the top level Thrive folder'
      exit 2
    end
  end

  base_path
end
