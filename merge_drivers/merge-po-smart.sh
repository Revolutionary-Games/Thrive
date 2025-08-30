#!/usr/bin/env bash
BASE="$1"
OURS="$2"
THEIRS="$3"

TMP=$(mktemp)
msgcat --use-first -o "$TMP" "$OURS" "$THEIRS"

date_from () {
  grep -m1 "$2" "$1" | sed -E 's/.*: ([0-9-]+ [0-9:+]+).*/\1/'
}

for FIELD in "PO-Revision-Date" "POT-Creation-Date"; do
  OUR_DATE=$(date_from "$OURS" "$FIELD")
  THEIR_DATE=$(date_from "$THEIRS" "$FIELD")

  if [[ "$OUR_DATE" > "$THEIR_DATE" ]]; then
    NEW_DATE="$OUR_DATE"
  else
    NEW_DATE="$THEIR_DATE"
  fi

  sed -i "s/^\"$FIELD:.*\"/\"$FIELD: $NEW_DATE\\\\n\"/" "$TMP"
done

cp "$TMP" "$OURS"
rm "$TMP"

exit 0
