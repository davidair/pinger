# Pinger

A collection of tools that record ping times from google.com to help troubleshoot intermittent connectivity issues.

## Getting started

The simplest way to use the pinger is to simply run either Pinger or PingerCLI projects.
Once you restore the nuget packages, they will run out of the box and store results in a SQLite database under %userprofile%\AppData\Roaming\Pinger\pings.sqlite.

The Windows Service uploads results to Firebase and also uses a weather service, meaning that you would need to:

1. Create a Firebase project and store its name under d:\Pinger\firebase-project-name.txt (or change the path in MainService.cs)
2. Ensure a real-time database is create for this project
3. Export a service account key for the Firebase project and store it under d:\Pinger\pinger-service-account-key.json (or change the path in MainService.cs)
4. Sign up for an API key at https://openweathermap.org/ and save it under D:\Pinger\weather_app_id.txt (or change the path in MainService.cs)

## Projects description

### Firebase

A basic Firebase site that uses Chart.js to render dropped ping percentage.
Also contains a Firebase function that aggregates dropped pings by a time internal (1 or 10 minutes)

### Pinger

A WPF UI app that runs the pings once a second, writes results to a SQLite database and reports overall stats.

### PingerCLI

A command-line app that runs the pings once a second, writes results to a SQLite database and reports overall stats.

### PingerCore

The core library for running pings and writing results to a SQLite database.

### PingerService

A Windows service that runs the pings once a second, writes results to a SQLite database and uploads them to Firebase alongside weather stats.

### TestCLI

A helper project for interacting with Firebase realtime database.

### WeatherCollector

A helper project for interacting with openweathermap.
