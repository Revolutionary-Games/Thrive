#!/usr/bin/env ruby
# frozen_string_literal: true

require 'json'
require 'nokogiri'
require 'open-uri'
require 'date'

require_relative '../RubySetupSystem/RubyCommon'

require_relative 'po_helpers'
require_relative 'json_helpers'
require_relative 'folder_detector'

DEVELOPERS_PAGE = 'https://wiki.revolutionarygamesstudio.com/wiki/Team_Members'
DONATIONS_PAGE = 'https://wiki.revolutionarygamesstudio.com/wiki/Donations'

# +31 is here to guarantee donations made in the last day of a month will be included properly
DONATION_DISPLAY_CUTOFF = (365 + 31) * 60 * 60 * 24

FILE_AGE_THRESHOLD = 60 * 60 * 24

PATRONS_FILE = 'scripts/patrons.json'
PATRONS_DOWNLOAD = 'https://dev.revolutionarygamesstudio.com/admin/patreon'

TRANSLATORS_FILE = 'scripts/translators.json'
TRANSLATORS_DOWNLOAD = 'https://translate.revolutionarygamesstudio.com/projects/thrive/thrive-game/#reports'
TRANSLATORS_EXTRA_INSTRUCTIONS = 'set start date to 1.1.2015 and end date to current date and format to JSON'

def check_file(path, download_url, extra = nil)
  if File.exist? path
    if (Time.now - File.mtime(path)).to_i > FILE_AGE_THRESHOLD
      puts 'The download file is too old. Please get a newer version'
    else
      return
    end
  end

  puts "A required file for credits generation is missing: #{path}, please download from:"
  puts download_url
  puts extra if extra
  exit 2
end

def fetch_wiki_developers
  page = Nokogiri::HTML(URI.parse(DEVELOPERS_PAGE).open)
  content = page.at_css('#bodyContent')

  in_section = :none
  team = :none

  groups = {}

  content.css('.mw-parser-output > *').each do |node|
    if node.name == 'h2'
      case node.content
      when 'Current Team'
        in_section = :current
      when 'Past Developers'
        in_section = :past
      when 'Outside Contributors'
        in_section = :outside
      end

      team = :none
      next
    end

    if node.name == 'h3'
      team = node.content
      next
    end

    next unless node.name == 'ul' && team != :none && in_section != :none

    groups[in_section] = {} unless groups[in_section]

    list = []

    node.css('li').each do |child|
      lead = child.inner_html.include?('<b>')

      list.append({ person: child.content.strip, lead: lead })
    end

    groups[in_section][team] = list
  end

  groups
end

def fetch_wiki_donations
  page = Nokogiri::HTML(URI.parse(DONATIONS_PAGE).open)
  content = page.at_css('#bodyContent')

  in_donators = false

  year = nil
  month = nil

  donations = {}

  content.css('.mw-parser-output > *').each do |node|
    if node.name == 'h2'
      in_donators = node.content == 'Donators'
      next
    end

    next unless in_donators

    if node.name == 'h3'
      year = Integer(node.content)
      month = nil
      next
    end

    if node.name == 'h4'
      month = node.content
      next
    end

    next unless node.name == 'ul' && !year.nil? && !month.nil?

    donations[year] = {} unless donations[year]

    list = []

    node.css('li').each do |child|
      list.append(child.content.strip)
    end

    donations[year][month] = list
  end

  donations
end

# Removes the old donations that shouldn't be listed in the credits anymore
def prune_old_donations(donations)
  now = Time.now.utc

  donations.select! do |year|
    donations[year].select!  do |month|
      time = Time.utc(year, Date::MONTHNAMES.index(month))

      (now - time).to_i < DONATION_DISPLAY_CUTOFF
    end

    donations[year].size.positive?
  end
end

def process_translators(translators)
  # For now all languages are rolled into one as many people have just fixed a basic thing in
  # a translation without being able to properly participate in it
  people = {}

  translators.each do |item|
    item.each do |_language, data|
      data.each do |user|
        name = user[1]

        # Skip deleted users in weblate
        next if user[0] == 'noreply+90@weblate.org' || name == 'Deleted User'

        # Ignore weblate admin account to make this a bit nicer
        next if name == 'Weblate Admin'

        # Might actually be translations, but anyway this is some activity metric
        words = user[2]

        people[name] = if !people.include? name
                         words
                       else
                         people[name] + words
                       end
      end
    end
  end

  # But we sort the translators based on the total words / translations they have done
  people.sort_by { |_k, v| v }.reverse.map { |i| i[0] }
end

def run
  base_path = detect_thrive_folder

  patrons_file = "#{base_path}#{PATRONS_FILE}"
  check_file patrons_file, PATRONS_DOWNLOAD
  patrons = JSON.parse(File.read(patrons_file))

  translators_file = "#{base_path}#{TRANSLATORS_FILE}"
  check_file translators_file, TRANSLATORS_DOWNLOAD, TRANSLATORS_EXTRA_INSTRUCTIONS
  translators = process_translators(JSON.parse(File.read(translators_file)))

  developers = fetch_wiki_developers

  donations = fetch_wiki_donations
  prune_old_donations(donations)

  json_file = "#{base_path}simulation_parameters/common/credits.json"

  File.write(
    json_file, JSON.generate(
                 { comment: 'This file is automatically generated by ' \
                   'retrieve_credits.rb! Part of the data is fetched from the Thrive ' \
                   'developer wiki', developers: developers, donations: donations,
                   translators: translators, patrons: patrons }
               )
  )

  unless jsonlint_on_file(json_file)
    puts 'Failed to run jsonlint'
    exit 2
  end

  puts "#{json_file} written"
end

run
