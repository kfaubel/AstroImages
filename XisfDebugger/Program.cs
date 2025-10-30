using System;
using System.IO;
using System.Text;
using System.Xml;

/// <summary>
/// Simple utility to debug XISF file structure issues
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: XisfDebugger <xisf-file-path>");
            return;
        }

        string filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        try
        {
            DebugXisfFile(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void DebugXisfFile(string filePath)
    {
        var buffer = File.ReadAllBytes(filePath);
        
        Console.WriteLine($"File size: {buffer.Length} bytes");
        
        // Check signature
        if (buffer.Length < 16)
        {
            Console.WriteLine("File too small to be XISF");
            return;
        }

        var signature = Encoding.ASCII.GetString(buffer, 0, 8);
        Console.WriteLine($"Signature: '{signature}'");
        
        if (signature != "XISF0100")
        {
            Console.WriteLine("Invalid XISF signature");
            return;
        }

        // Read header length
        int headerLength = BitConverter.ToInt32(buffer, 8);
        Console.WriteLine($"Header length: {headerLength}");

        // Read reserved field
        int reserved = BitConverter.ToInt32(buffer, 12);
        Console.WriteLine($"Reserved field: {reserved}");

        if (headerLength <= 0 || headerLength > buffer.Length - 16)
        {
            Console.WriteLine($"Invalid header length: {headerLength}");
            return;
        }

        // Extract header XML
        string headerXml = Encoding.UTF8.GetString(buffer, 16, headerLength);
        Console.WriteLine("\n--- Header XML ---");
        Console.WriteLine(headerXml);
        Console.WriteLine("--- End Header XML ---\n");

        // Parse XML
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(headerXml);
            
            // Look for Image element
            var imageNode = xmlDoc.SelectSingleNode("//Image");
            if (imageNode == null)
            {
                Console.WriteLine("ERROR: No Image element found in XML header");
                
                // List all elements
                Console.WriteLine("Available elements:");
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    Console.WriteLine($"  - {node.Name}");
                }
            }
            else
            {
                Console.WriteLine("Image element found:");
                foreach (XmlAttribute attr in imageNode.Attributes)
                {
                    Console.WriteLine($"  {attr.Name} = {attr.Value}");
                }
            }
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"XML parsing error: {ex.Message}");
        }
    }
}
