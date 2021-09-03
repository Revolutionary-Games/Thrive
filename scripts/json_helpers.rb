def jsonlint_on_file(path)
  if runSystemSafe('jsonlint', '-i', path, '--indent', '    ') != 0
    OUTPUT_MUTEX.synchronize do
      error 'JSONLint failed on file'
    end
    return false
  end
  true
end
