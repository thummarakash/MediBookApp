# MediBook - Professional .NET MAUI Android App

MediBook is a professional clinic appointment app built with C# and XAML using .NET MAUI.

## Important build instruction

This version is Android-only to avoid the NuGet target error in Visual Studio when building on an Android emulator/device.

If Visual Studio shows an error like `project.assets.json does not have a target for net10.0-android`, close Visual Studio and delete the `bin` and `obj` folders inside `MediBookApp`, then reopen the solution and restore NuGet packages.

You can also run `CLEAN_RESTORE_BUILD.bat` from this folder.

## Run steps in Visual Studio 2026

1. Extract the ZIP into a new clean folder. Do not extract over the old folder.
2. Open `MediBook.sln`.
3. Right-click the solution and select `Restore NuGet Packages`.
4. Select your Android emulator/device.
5. Click `Build > Clean Solution`.
6. Click `Build > Rebuild Solution`.
7. Click Run.

## Native features included

- Camera/photo upload for medical documents.
- Local SQLite database for users, doctors, appointments, documents, and email reminders.
- Phone dialer to contact the clinic.
- Email composer for appointment confirmation and reminder emails.
- Maps/directions to clinic location.
- Google sign-up demo mode with real OAuth structure ready in `Services/GoogleAuthService.cs`.

## Database

The local database file is created automatically as `medibook.db3` in the app data folder. Doctor seed data is inserted automatically on first launch.

## Email note

Mobile apps cannot safely send automatic background emails directly without an email backend or SMTP/API credentials. This app opens the native email composer and stores appointment-day reminders in SQLite.
