require_relative '../RubySetupSystem/RubyCommon'

puts 'Extracting .pot file'
runOpen3Checked 'pybabel', 'extract', '-F', 'babelrc', '-k', 'LineEdit', '-k', 'text', '-k', 'window_title', '-k', 'dialog_text', '-k', 'Translate', '-o', 'messages.pot', '../.'
puts 'Done extracting'

puts 'Extracting .po files'
runOpen3Checked 'msgmerge', '--update', '--backup=none', 'en.po', 'messages.pot'
runOpen3Checked 'msgmerge', '--update', '--backup=none', 'fr.po', 'messages.pot'
success 'Done extracting .po files'
