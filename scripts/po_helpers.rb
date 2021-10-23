MSG_ID_REGEX = /^msgid "(.*)"$/.freeze
FUZZY_TRANSLATION_REGEX = /^#, fuzzy/.freeze
PLAIN_QUOTED_MESSAGE = /^"(.*)"/.freeze
GETTEXT_HEADER_NAME = /^([\w-]+):\s+/.freeze

def find_next_msg_id(reader)
  loop do
    line = reader.gets

    if line.nil?
      # File ended
      return nil
    end

    matches = line.match(MSG_ID_REGEX)

    return matches[1] if matches
  end
end

def read_gettext_header_order(reader)
  expected_header_msg = find_next_msg_id reader

  onError 'File ended when looking for gettext header' if expected_header_msg.nil?

  if expected_header_msg != ''
    error 'Could not find gettext header, expected blank msg id, ' \
          "but got: #{expected_header_msg}"
    return ['header not found...']
  end

  headers = []

  # Read content lines
  loop do
    line = reader.gets

    break if line.nil?

    break if line.strip.empty?

    matches = line.match(PLAIN_QUOTED_MESSAGE)

    next unless matches

    matches = matches[1].match(GETTEXT_HEADER_NAME)

    headers.push matches[1] if matches
  end

  headers
end
