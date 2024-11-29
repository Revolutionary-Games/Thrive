# A tool to find differences between two objects
class_name GdDiffTool
extends RefCounted


const DIV_ADD :int = 214
const DIV_SUB :int = 215


static func _diff(lb: PackedByteArray, rb: PackedByteArray, lookup: Array[Array], ldiff: Array, rdiff: Array) -> void:
	var loffset := lb.size()
	var roffset := rb.size()

	while true:
		#if last character of X and Y matches
		if loffset > 0 && roffset > 0 && lb[loffset - 1] == rb[roffset - 1]:
			loffset -= 1
			roffset -= 1
			ldiff.push_front(lb[loffset])
			rdiff.push_front(rb[roffset])
			continue
		#current character of Y is not present in X
		else: if (roffset > 0 && (loffset == 0 || lookup[loffset][roffset - 1] >= lookup[loffset - 1][roffset])):
			roffset -= 1
			ldiff.push_front(rb[roffset])
			ldiff.push_front(DIV_ADD)
			rdiff.push_front(rb[roffset])
			rdiff.push_front(DIV_SUB)
			continue
		#current character of X is not present in Y
		else: if (loffset > 0 && (roffset == 0 || lookup[loffset][roffset - 1] < lookup[loffset - 1][roffset])):
			loffset -= 1
			ldiff.push_front(lb[loffset])
			ldiff.push_front(DIV_SUB)
			rdiff.push_front(lb[loffset])
			rdiff.push_front(DIV_ADD)
			continue
		break


# lookup[i][j] stores the length of LCS of substring X[0..i-1], Y[0..j-1]
static func _createLookUp(lb: PackedByteArray, rb: PackedByteArray) -> Array[Array]:
	var lookup: Array[Array] = []
	@warning_ignore("return_value_discarded")
	lookup.resize(lb.size() + 1)
	for i in lookup.size():
		var x := []
		@warning_ignore("return_value_discarded")
		x.resize(rb.size() + 1)
		lookup[i] = x
	return lookup


static func _buildLookup(lb: PackedByteArray, rb: PackedByteArray) -> Array[Array]:
	var lookup := _createLookUp(lb, rb)
	# first column of the lookup table will be all 0
	for i in lookup.size():
		lookup[i][0] = 0
	# first row of the lookup table will be all 0
	for j :int in lookup[0].size():
		lookup[0][j] = 0

	# fill the lookup table in bottom-up manner
	for i in range(1, lookup.size()):
		for j in range(1, lookup[0].size()):
			# if current character of left and right matches
			if lb[i - 1] == rb[j - 1]:
				lookup[i][j] = lookup[i - 1][j - 1] + 1;
			# else if current character of left and right don't match
			else:
				lookup[i][j] = max(lookup[i - 1][j], lookup[i][j - 1]);
	return lookup


static func string_diff(left :Variant, right :Variant) -> Array[PackedByteArray]:
	var lb := PackedByteArray() if left == null else str(left).to_utf8_buffer()
	var rb := PackedByteArray() if right == null else str(right).to_utf8_buffer()
	var ldiff := Array()
	var rdiff := Array()
	var lookup := _buildLookup(lb, rb);
	_diff(lb, rb, lookup, ldiff, rdiff)
	return [PackedByteArray(ldiff), PackedByteArray(rdiff)]


# prototype
static func longestCommonSubsequence(text1 :String, text2 :String) -> PackedStringArray:
	var text1Words := text1.split(" ")
	var text2Words := text2.split(" ")
	var text1WordCount := text1Words.size()
	var text2WordCount := text2Words.size()
	var solutionMatrix := Array()
	for i in text1WordCount+1:
		var ar := Array()
		for n in text2WordCount+1:
			ar.append(0)
		solutionMatrix.append(ar)

	for i in range(text1WordCount-1, 0, -1):
		for j in range(text2WordCount-1, 0, -1):
			if text1Words[i] == text2Words[j]:
				solutionMatrix[i][j] = solutionMatrix[i + 1][j + 1] + 1;
			else:
				solutionMatrix[i][j] = max(solutionMatrix[i + 1][j], solutionMatrix[i][j + 1]);

	var i := 0
	var j := 0
	var lcsResultList := PackedStringArray();
	while (i < text1WordCount && j < text2WordCount):
		if text1Words[i] == text2Words[j]:
			@warning_ignore("return_value_discarded")
			lcsResultList.append(text2Words[j])
			i += 1
			j += 1
		else: if (solutionMatrix[i + 1][j] >= solutionMatrix[i][j + 1]):
			i += 1
		else:
			j += 1
	return lcsResultList


static func markTextDifferences(text1 :String, text2 :String, lcsList :PackedStringArray, insertColor :Color, deleteColor:Color) -> String:
	var stringBuffer := ""
	if text1 == null and lcsList == null:
		return stringBuffer

	var text1Words := text1.split(" ")
	var text2Words := text2.split(" ")
	var i := 0
	var j := 0
	var word1LastIndex := 0
	var word2LastIndex := 0
	for k in lcsList.size():
		while i < text1Words.size() and j < text2Words.size():
			if text1Words[i] == lcsList[k] and text2Words[j] == lcsList[k]:
				stringBuffer += "<SPAN>" + lcsList[k] + " </SPAN>"
				word1LastIndex = i + 1
				word2LastIndex = j + 1
				i = text1Words.size()
				j = text2Words.size()

			else: if text1Words[i] != lcsList[k]:
				while i < text1Words.size() and text1Words[i] != lcsList[k]:
					stringBuffer += "<SPAN style='BACKGROUND-COLOR:" + deleteColor.to_html() + "'>" + text1Words[i] + " </SPAN>"
					i += 1
			else: if text2Words[j] != lcsList[k]:
				while j < text2Words.size() and text2Words[j] != lcsList[k]:
					stringBuffer += "<SPAN style='BACKGROUND-COLOR:" + insertColor.to_html() + "'>" + text2Words[j] + " </SPAN>"
					j += 1
			i = word1LastIndex
			j = word2LastIndex

			while word1LastIndex < text1Words.size():
				stringBuffer += "<SPAN style='BACKGROUND-COLOR:" + deleteColor.to_html() + "'>" + text1Words[word1LastIndex] + " </SPAN>"
				word1LastIndex += 1
			while word2LastIndex < text2Words.size():
				stringBuffer += "<SPAN style='BACKGROUND-COLOR:" + insertColor.to_html() + "'>" + text2Words[word2LastIndex] + " </SPAN>"
				word2LastIndex += 1
	return stringBuffer
