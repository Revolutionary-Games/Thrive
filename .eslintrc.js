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
        ]
    }
};
