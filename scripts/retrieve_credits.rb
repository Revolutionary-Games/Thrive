require 'json'
require 'nokogiri'
require 'open-uri'

require_relative '../RubySetupSystem/RubyCommon'

require_relative 'po_helpers'
require_relative 'json_helpers'
require_relative 'folder_detector'

DEVELOPERS_PAGE = 'https://wiki.revolutionarygamesstudio.com/wiki/Team_Members'

def fetch_wiki_developers
  page = Nokogiri::HTML(URI.open(DEVELOPERS_PAGE))
  content = page.at_css('#bodyContent')

  content.css('.mw-parser-output > *').each do |node|
    puts node.name
  end

  nil
end

def run
  base_path = detect_thrive_folder

  developers = fetch_wiki_developers
  puts developers
end

run
