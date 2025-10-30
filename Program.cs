using System;
using System.IO;
using AstroImages.Utils;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: XisfTestDebug <path-to-xisf-file>");
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
            var bytes = File.ReadAllBytes(filePath);
            Console.WriteLine($"File: {Path.GetFileName(filePath)}");
            Console.WriteLine($"Size: {bytes.Length} bytes");
            
            // Check if it's valid XISF
            bool isXisf = XisfUtilities.IsXisfData(bytes);
            Console.WriteLine($"Is XISF: {isXisf}");
            
            if (isXisf)
            {
                // Parse metadata to see what we have
                var metadata = XisfParser.ParseMetadata(bytes);
                Console.WriteLine($"Metadata entries: {metadata.Count}");
                
                foreach (var kvp in metadata)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                
                // Try to get channel info
                Console.WriteLine("\nTesting channel detection...");
                TestChannelDetection(bytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static void TestChannelDetection(byte[] buffer)
    {
        try
        {
            // Extract XML header manually (same logic as channel detection)
            if (buffer.Length < 8)
            {
                Console.WriteLine("Buffer too small");
                return;
            }
                
            // XISF signature is first 8 bytes: "XISF0100"
            string signature = System.Text.Encoding.ASCII.GetString(buffer, 0, 8);
            Console.WriteLine($"Signature: {signature}");
            
            // XML header follows immediately after
            int xmlStart = 8;
            int xmlEnd = -1;
            
            // Find the null terminator that ends the XML header
            for (int i = xmlStart; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    xmlEnd = i;
                    break;
                }
            }
            
            if (xmlEnd == -1)
            {
                Console.WriteLine("No XML terminator found");
                return;
            }
                
            // Extract and parse XML
            var headerXml = System.Text.Encoding.UTF8.GetString(buffer, xmlStart, xmlEnd - xmlStart);
            Console.WriteLine($"XML Header:\n{headerXml}");
            
            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.LoadXml(headerXml);
            
            // Find Image element
            var imageNode = xmlDoc.SelectSingleNode("//Image");
            if (imageNode?.Attributes?["geometry"]?.Value is string geometry)
            {
                Console.WriteLine($"Geometry: {geometry}");
                var parts = geometry.Split(':');
                if (parts.Length > 2 && int.TryParse(parts[2], out int channels))
                {
                    Console.WriteLine($"Channels detected: {channels}");
                }
            }
            else
            {
                Console.WriteLine("No Image element or geometry found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Channel detection error: {ex.Message}");
        }
    }
}