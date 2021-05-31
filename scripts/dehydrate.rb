# frozen_string_literal: true

require 'json'
require 'set'

# Commmon dehydrate helpers
DEHYDRATE_CACHE = 'builds/dehydrate_cache'
DEVBUILDS_FOLDER = 'builds/devbuilds'

DEHYDRATE_FILE_SIZE_THRESSHOLD = 100_000

def git_commit
  `git rev-parse --verify HEAD`.strip
end

def git_branch
  return ENV['CI_BRANCH'] if ENV['CI_BRANCH']

  `git rev-parse --symbolic-full-name --abbrev-ref HEAD`.strip
end

def pck_tool
  if OS.windows?
    'godotpcktool.exe'
  else
    'godotpcktool'
  end
end

# Dehydrates a file by moving it to the dehydrate cache if needed
def dehydrate_file(file, cache)
  hash = SHA3::Digest::SHA256.file(file).hexdigest

  target = File.join DEHYDRATE_CACHE, "#{hash}.gz"

  # Only copy to the dehydrate cache if hash doesn't exist
  gzip_to_target file, target unless File.exist? target

  cache.add file, hash

  FileUtils.rm file
end

# Checks if a file needs to be dehydrated
def check_dehydrate_file(file, cache)
  return if file =~ /.+\.pck$/

  return if File.size(file) < DEHYDRATE_FILE_SIZE_THRESSHOLD

  dehydrate_file file, cache
end

# Cache helper for dehydrate operations
class DehydrateCache
  attr_reader :base_folder

  def initialize(base_folder)
    @base_folder = base_folder
    @data = {}
  end

  def add(path, hash)
    @data[process_path path] = {
      sha3: hash
    }
  end

  def add_pck(path, pck_cache)
    @data[process_path path] = {
      type: 'pck',
      data: pck_cache
    }
  end

  # Returns all object hashes in this cache
  def hashes
    result = Set.new

    @data.each do |_, obj|
      result.add obj[:sha3] if obj[:sha3]
      result.merge obj[:data].hashes if obj[:data]
    end

    result.to_a
  end

  def process_path(path)
    raise 'path has backlashes' if path.include? '\\'

    path.delete_prefix(@base_folder).delete_prefix('/')
  end

  def to_json(options = {})
    JSON.generate({ files: @data }, options)
  end
end
