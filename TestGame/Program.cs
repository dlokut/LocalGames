Console.WriteLine("Hello, World!");

var testEnv = Environment.GetEnvironmentVariable("test");
if (testEnv != null) Console.WriteLine(testEnv);

Console.WriteLine("This is a test game, press any key to exit");
Console.ReadLine();
