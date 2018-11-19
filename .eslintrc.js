module.exports = {
    "env": {
        "browser": true,
        "es6": true,
        "jquery": true
    },
    "plugins": [
        "html"
    ],
    "extends": "eslint:recommended",
    "parserOptions": {
        "ecmaVersion": 2018,
        "sourceType": "module"
    },
    // These are from Leviathan and Thrive JavaScript support
    "globals": {
        // Leviathan
        "Leviathan": true,

        // Thrive
        "Thrive": true,
    },
    "rules": {
        "indent": [
            "error",
            4
        ],
        "max-len": [
            "error",
            { "code": 95 }
        ],
        "linebreak-style": [
            "error",
            "unix"
        ],
        "quotes": [
            "error",
            "double"
        ],
        "semi": [
            "error",
            "always"
        ],
        "no-console": [
            "error", {
                allow: ["warn", "error"]
            }
        ],
        "curly": "off",
        "dot-location": ["error", "object"],
        "dot-notation": "error",
        "no-eval": "error",
        "no-implied-eval": "error",
        "strict": "error",
        "block-spacing": "error",
        "brace-style": "error",
        "no-template-curly-in-string": "error",
        "no-misleading-character-class": "error",
        "no-extra-parens": [
            "error",
            "all",
            { "nestedBinaryExpressions": false }
        ],
        "arrow-parens": ["error", "always"],
        "consistent-return": "error",
        "block-scoped-var": "error",
        "no-extra-bind": "error",
        "no-extra-label": "error",
        "no-labels": ["error", { "allowLoop": true }],
        "no-multi-spaces": "error",
        "no-return-assign": "error",
        "no-sequences": "error",
        "no-unmodified-loop-condition": "error",
        "no-useless-return": "error",
        "prefer-promise-reject-errors": "error",
        "yoda": "error",
        "init-declarations": ["error", "always"],
        "no-undef-init": "error",
        "eol-last": ["error", "always"],
        "lines-between-class-members": ["error", "always"],
        "lines-around-comment": [
            "error",
            {
                "beforeLineComment": true, "allowBlockStart": true,
                "allowObjectStart": true, "allowArrayStart": true
            }
        ],
        "key-spacing": [
            "error",
            { "afterColon": true, "beforeColon": false  }
        ],
        "function-paren-newline": ["error", "never"],
        "func-call-spacing": ["error", "never"],
        "computed-property-spacing": ["error", "never"],
        "comma-spacing": ["error", { "before": false, "after": true }],
        // This is bad for commented out code (use eslint disable for those lines)
        "capitalized-comments": [
            "error",
            "always",
            {
                "ignorePattern": "pragma|ignored",
                "ignoreInlineComments": true,
                "ignoreConsecutiveComments": true
            }
        ],
        "spaced-comment": [
            "error", "always",
            {
                "markers": ["/", "!"],
                "exceptions": ["-", "+", "*"]
            }
        ],
        "array-bracket-spacing": ["error", "never"],
        "array-bracket-newline": [
            "error",
            { "multiline": true }
        ],
        "comma-style": ["error", "last"],
        "multiline-comment-style": ["error", "separate-lines"],
        "new-parens": "error",
        "no-lonely-if": "error",
        "no-multiple-empty-lines": [
            "error",
            { "max": 4, "maxEOF": 1 }
        ],
        "no-trailing-spaces": "error",
        "no-unneeded-ternary": "error",
        "no-whitespace-before-property": "error",
        "operator-linebreak": ["error", "after"],
        "semi-style": ["error", "last"],
        "semi-spacing": "error",
        "space-infix-ops": "error",
        "space-unary-ops": [
            "error",
            {"words": true, "nonwords": false}
        ],
        "switch-colon-spacing": "error",
        "no-var": "error",
        "prefer-const": "error",
        "prefer-spread": "error",
        "rest-spread-spacing": ["error", "never"],
        "sort-imports": "error",
        "yield-star-spacing": ["error", "after"],
        "object-curly-spacing": ["error", "never"],
        "object-curly-newline": [
            "error",
            { "multiline": true }
        ],
        "consistent-this": ["error", "that"],
        "prefer-rest-params": "error",
        "padding-line-between-statements": [
            "error",
            { blankLine: "always", prev: "*", next: "return" },
            { blankLine: "any", prev: "expression", next: "return" },
            // Blank lines after each block of declarations
            { blankLine: "always", prev: ["const", "let", "var"], next: "*"},
            { blankLine: "any",    prev: ["const", "let", "var"], next:
              ["const", "let", "var", "expression"]},
            { blankLine: "always", prev: "directive", next: "*" },
            { blankLine: "any",    prev: "directive", next: "directive" },
        ]
    }
};
