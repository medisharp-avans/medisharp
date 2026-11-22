// Every database and end-to-end test resets the one shared test database,
// so tests must not run at the same time or they would wipe each other's data.
[assembly: DoNotParallelize]
