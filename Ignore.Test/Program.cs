namespace Ignore.Test;

internal class Program {
    static async Task Main(string[] args) {
        Console.WriteLine("Enter values (one per line). Type 'END' to finish:");
        List<string> values = new();
        string input;

        while((input = Console.ReadLine()) != "END") {
            values.Add(input);
        }

        // Get multiline text from the user
        Console.WriteLine("Enter the text to search (type 'END' on a new line to finish):");
        List<string> textLines = new();

        while(true) {
            string line = Console.ReadLine();
            if(line == "END") {
                break; // End on a line with 'END'
            }

            textLines.Add(line);
        }

        // Combine the lines into a single string
        string text = string.Join(Environment.NewLine, textLines);

        // Find and output values not found in the text
        List<string> notFoundValues = new();

        foreach(string value in values) {
            if(!text.Contains("<" + value + ">")) {
                notFoundValues.Add(value);
            }
        }

        Console.WriteLine("Values not found in the text:");
        foreach(string notFound in notFoundValues) {
            Console.WriteLine(notFound);
        }
    }
}