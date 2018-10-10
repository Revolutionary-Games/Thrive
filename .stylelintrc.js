"use strict";

// This is a combined style of the recommended and standard styles
/*
  The MIT License (MIT)

  Copyright (c) 2017 stylelint

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all
  copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/

module.exports = {
    rules: {
        "at-rule-no-unknown": true,
        "block-no-empty": true,
        "color-no-invalid-hex": true,
        "comment-no-empty": true,
        "declaration-block-no-duplicate-properties": [
            true,
            {
                ignore: ["consecutive-duplicates-with-different-values"]
            }
        ],
        "declaration-block-no-shorthand-property-overrides": true,
        "font-family-no-duplicate-names": true,
        "font-family-no-missing-generic-family-keyword": null,
        "function-calc-no-unspaced-operator": true,
        "function-linear-gradient-no-nonstandard-direction": true,
        "keyframe-declaration-no-important": true,
        "media-feature-name-no-unknown": true,
        "no-descending-specificity": true,
        "no-duplicate-at-import-rules": true,
        "no-duplicate-selectors": true,
        "no-empty-source": true,
        "no-extra-semicolons": true,
        "no-invalid-double-slash-comments": true,
        "property-no-unknown": true,
        "selector-pseudo-class-no-unknown": true,
        "selector-pseudo-element-no-unknown": true,
        "selector-type-no-unknown": true,
        "string-no-newline": true,
        "unit-no-unknown": true,
        "at-rule-empty-line-before": [ "always", {
            except: [
                "blockless-after-same-name-blockless",
                "first-nested",
            ],
            ignore: ["after-comment"],
        } ],
        "at-rule-name-case": "lower",
        "at-rule-name-space-after": "always-single-line",
        "at-rule-semicolon-newline-after": "always",
        "block-closing-brace-empty-line-before": "never",
        "block-closing-brace-newline-after": "always",
        "block-closing-brace-newline-before": "always-multi-line",
        "block-closing-brace-space-before": "always-single-line",
        "block-opening-brace-newline-after": "always-multi-line",
        "block-opening-brace-space-after": "always-single-line",
        "block-opening-brace-space-before": "always",
        "color-hex-case": "lower",
        "color-hex-length": "short",
        "comment-empty-line-before": [ "always", {
            except: ["first-nested"],
            ignore: ["stylelint-commands"],
        } ],
        "comment-whitespace-inside": "always",
        "custom-property-empty-line-before": [ "always", {
            except: [
                "after-custom-property",
                "first-nested",
            ],
            ignore: [
                "after-comment",
                "inside-single-line-block",
            ],
        } ],
        "declaration-bang-space-after": "never",
        "declaration-bang-space-before": "always",
        "declaration-block-semicolon-newline-after": "always-multi-line",
        "declaration-block-semicolon-space-after": "always-single-line",
        "declaration-block-semicolon-space-before": "never",
        "declaration-block-single-line-max-declarations": 1,
        "declaration-block-trailing-semicolon": "always",
        "declaration-colon-newline-after": "always-multi-line",
        "declaration-colon-space-after": "always-single-line",
        "declaration-colon-space-before": "never",
        "declaration-empty-line-before": [ "always", {
            except: [
                "after-declaration",
                "first-nested",
            ],
            ignore: [
                "after-comment",
                "inside-single-line-block",
            ],
        } ],
        "function-comma-newline-after": "always-multi-line",
        "function-comma-space-after": "always-single-line",
        "function-comma-space-before": "never",
        "function-max-empty-lines": 0,
        "function-name-case": "lower",
        "function-parentheses-newline-inside": "always-multi-line",
        "function-parentheses-space-inside": "never-single-line",
        "function-whitespace-after": "always",
        "indentation": 2,
        "length-zero-no-unit": true,
        "max-empty-lines": 3,
        "media-feature-colon-space-after": "always",
        "media-feature-colon-space-before": "never",
        "media-feature-name-case": "lower",
        "media-feature-parentheses-space-inside": "never",
        "media-feature-range-operator-space-after": "always",
        "media-feature-range-operator-space-before": "always",
        "media-query-list-comma-newline-after": "always-multi-line",
        "media-query-list-comma-space-after": "always-single-line",
        "media-query-list-comma-space-before": "never",
        "no-eol-whitespace": true,
        "no-missing-end-of-source-newline": true,
        "number-leading-zero": "always",
        "number-no-trailing-zeros": true,
        "property-case": "lower",
        "rule-empty-line-before": [ "always-multi-line", {
            except: ["first-nested"],
            ignore: ["after-comment"],
        } ],
        "selector-attribute-brackets-space-inside": "never",
        "selector-attribute-operator-space-after": "never",
        "selector-attribute-operator-space-before": "never",
        "selector-combinator-space-after": "always",
        "selector-combinator-space-before": "always",
        "selector-descendant-combinator-no-non-space": true,
        "selector-list-comma-newline-after": "always",
        "selector-list-comma-space-before": "never",
        "selector-max-empty-lines": 0,
        "selector-pseudo-class-case": "lower",
        "selector-pseudo-class-parentheses-space-inside": "never",
        "selector-pseudo-element-case": "lower",
        "selector-pseudo-element-colon-notation": "double",
        "selector-type-case": "lower",
        "unit-case": "lower",
        "value-list-comma-newline-after": "always-multi-line",
        "value-list-comma-space-after": "always-single-line",
        "value-list-comma-space-before": "never",
        "value-list-max-empty-lines": 0,      
    }
};
