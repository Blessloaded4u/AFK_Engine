# AFK Engine — Level Up in the Real World

AFK Engine is an AI-powered, real-world progression system built around a simple idea: technology should help people take meaningful action beyond the screen.

Instead of trying to keep users inside an app, AFK Engine generates personalized missions that encourage users to explore interests, practice skills, work toward aspirations, and participate in real-world activities.

> **The better AFK Engine works, the less time you should need to spend using it.**

## Project at a glance

- **Project:** AFK Engine
- **Purpose:** Encourage real-world activity through personalized missions and game-inspired progression.
- **AI integration:** Groq API using `openai/gpt-oss-20b`.
- **Core loop:** Set up a profile → receive a personalized mission → do the activity away from the screen → report the activity → earn XP and track progress.
- **Status:** Working prototype / ongoing development. Features and setup may change as the project evolves.

## Demo

- **Live application:** https://afk-engine.onrender.com/
- **Demo video:** https://youtu.be/djrKp_9NWqM?si=FzZNeWJSWN_mIJWe
- **DEV article:** https://dev.to/blessloaded/afk-engine-what-if-an-apps-success-meant-you-spent-less-time-using-it-78c

## Why AFK Engine?

Digital products often compete for people's attention. AFK Engine explores a different approach: use AI to help people decide what to do next in the real world, then let the app get out of the way.

Users can provide information about their interests, aspirations, dislikes, and personal context. The AI uses that context to suggest missions that may feel more relevant than a generic checklist.

## How it works

1. **Personalize:** The user provides context about their interests, aspirations, and preferences.
2. **Generate a mission:** The AI suggests a mission based on the user's context.
3. **Review the mission:** A mission can include a name, description, duration, difficulty, XP reward, and reason for the suggestion.
4. **Take action offline:** The user leaves the app and attempts the real-world activity.
5. **Report completion:** The user returns and reports what they did.
6. **Progress:** The application updates progression according to its rules.

### AI and progression

AFK Engine uses AI to personalize mission suggestions. The application—not the AI response—should remain responsible for validating mission data and applying progression rules.

The current progression design awards between **10 and 50 XP**, based on mission difficulty. Confirm these values against the current implementation before relying on them in a release.

### Important limitation

Mission completion is currently based on the user's report. AFK Engine does **not** claim to independently verify that every real-world activity occurred.

## Technology

Known AI integration:

- **Groq API**
- **Model:** `openai/gpt-oss-20b`

Update this list to match the actual codebase:

- Frontend/framework: [Add actual framework]
- Backend/API: [Add actual backend, if applicable]
- Database/storage: [Add actual database or storage, if applicable]
- Hosting/deployment: [Add hosting provider]

## Getting started

The exact commands depend on the framework and deployment setup in this repository.  the typical starting point is:

1. Install the supported .NET version.
2. Clone the repository.
3. Install dependencies using the package manager and lockfile in the repository.
4. Build the program and run the program

Example commands for a typical npm-based project:

```bash
git clone <YOUR_GITHUB_REPOSITORY_URL>
cd <YOUR_PROJECT_FOLDER>
dotnet build
dotnet run


## Environment variables

Copy `.env.example` to `.env` for local development and replace placeholder values with your own credentials.

- `GROQ_API_KEY` — your private Groq API key.
- `GROQ_MODEL` — the model identifier used for mission generation.

Keep `.env` out of version control. Never publish API keys, access tokens, passwords, or production secrets.

**Security note:** If the browser/frontend calls Groq directly with a secret key, move that call behind a server-side API route or backend. Do not expose private API keys in client-side code or variables intended to be bundled into the browser (for example, `VITE_*` variables).

## Project structure

The structure below is illustrative. Replace it with the actual folders and files in this repository.

```text
AFK-Engine/
├── public/                 # Static assets, if used
├── src/                    # Application source code, if used
├── .env.example            # Safe template for required environment variables
├── .gitignore              # Files excluded from version control
└── README.md               # Project documentation
```

## Responsible use and privacy

- Ask only for information needed to personalize missions.
- Do not include private user data or secrets in logs, screenshots, or public examples.
- Review AI-generated missions for safety and appropriateness before presenting them to users.
- Be transparent that reported activity is self-reported unless independent verification is implemented.

## Possible future improvements

- Improve mission quality, relevance, and safety.
- Expand progress summaries and mission history.
- Improve error handling and API reliability.
- Add tests for mission validation and progression rules.
- Improve accessibility, mobile usability, and onboarding.
- Document the architecture and contribution workflow.

These are possible improvements, not claims that the features are already implemented.

## Contributing and feedback

Feedback, bug reports, and improvement ideas are welcome. When reporting a bug, include clear reproduction steps and relevant error messages without sharing secrets or private data.

## License

No license has been specified here. Add a `LICENSE` file before describing this project as open source or granting permission for others to reuse, modify, or redistribute the code. Choose a license that matches your intentions.

## Author

Created as an experiment in using AI and gamification to help people take action beyond the screen.

**AFK Engine — Level up in the real world.**
