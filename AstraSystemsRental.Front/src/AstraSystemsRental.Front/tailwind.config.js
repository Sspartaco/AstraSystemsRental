/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Features/**/*.cshtml",
    "./Shared/**/*.cshtml",
    "./wwwroot/js/**/*.js",
    "./node_modules/flowbite/**/*.js"
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        bg: {
          DEFAULT: "#08090d",
          2: "#0a0c12",
          card: "#0e1119",
          panel: "#101521",
          panel2: "#141a28"
        },
        brand: {
          DEFAULT: "#4f7cff",
          bright: "#6e93ff",
          deep: "#2d4ed8"
        },
        accent: "#3ddc97",
        warn: "#ffb454",
        danger: "#ff6b6b",
        navy: "#1b2440",
        steel: "#1a2032",
        content: {
          DEFAULT: "#e7e9f0",
          soft: "#9aa3bd",
          muted: "#5d6685",
          faint: "#3a4258"
        }
      },
      fontFamily: {
        display: ["Space Grotesk", "sans-serif"],
        body: ["Inter", "sans-serif"]
      },
      fontSize: {
        "2xs": ".625rem",
        xs: ".6875rem",
        sm: ".8125rem",
        base: ".9375rem",
        lg: "1.0625rem",
        xl: "1.25rem",
        "2xl": "1.5rem",
        "3xl": "1.95rem",
        "4xl": "2.6rem",
        "5xl": "3.4rem"
      },
      borderRadius: {
        sm: ".375rem",
        md: ".625rem",
        lg: "1rem",
        xl: "1.375rem",
        "2xl": "1.75rem"
      },
      boxShadow: {
        card: "0 4px 32px rgba(0,0,0,0.5)",
        panel: "0 30px 80px -20px rgba(0,0,0,0.75), 0 0 0 1px rgba(255,255,255,0.05)",
        brand: "0 8px 40px rgba(79,124,255,0.28)",
        glow: "0 0 24px rgba(79,124,255,0.2)"
      },
      backgroundImage: {
        "gradient-brand": "linear-gradient(135deg, #6e93ff 0%, #4f7cff 45%, #2d4ed8 100%)",
        "gradient-title": "linear-gradient(120deg, #ffffff 0%, #c7d0ec 50%, #7e93d6 100%)",
        "gradient-mesh":
          "radial-gradient(60% 50% at 75% 30%, rgba(79,124,255,0.16) 0%, transparent 60%), radial-gradient(50% 50% at 15% 80%, rgba(61,220,151,0.07) 0%, transparent 55%)"
      },
      transitionTimingFunction: {
        spring: "cubic-bezier(.16,1,.3,1)"
      }
    }
  },
  plugins: [require("flowbite/plugin")]
};
