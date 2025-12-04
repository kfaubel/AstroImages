using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AstroImages.Utils
{
    /// <summary>
    /// Professional-grade XISF (Extensible Image Serialization Format) parser implementing 
    /// basic XISF 1.0 specification support for monolithic files without compression.
    /// Provides comprehensive metadata extraction and image reading capabilities.
    /// </summary>
    public static class XisfParser
    {
        private const string XISF_SIGNATURE = "XISF0100";
        
        /// <summary>
        /// Parse XISF metadata from file header only (optimized for large files)
        /// </summary>
        public static Dictionary<string, object> ParseMetadataFromFile(string filePath)
        {
            var metadata = new Dictionary<string, object>();
            
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // Read signature (8 bytes)
                    byte[] signature = new byte[8];
                    fileStream.Read(signature, 0, 8);
                    
                    if (Encoding.ASCII.GetString(signature) != XISF_SIGNATURE)
                    {
                        throw new InvalidOperationException("Invalid XISF file signature");
                    }
                    
                    // Read header length (4 bytes, little-endian)
                    byte[] lengthBytes = new byte[4];
                    fileStream.Read(lengthBytes, 0, 4);
                    int headerLength = BitConverter.ToInt32(lengthBytes, 0);
                    
                    if (headerLength <= 0 || headerLength > 100 * 1024 * 1024) // Sanity check: max 100MB header
                    {
                        throw new InvalidOperationException($"Invalid header length: {headerLength}");
                    }
                    
                    // Skip reserved field (4 bytes)
                    fileStream.Seek(4, SeekOrigin.Current);
                    
                    // Read header XML
                    byte[] headerBuffer = new byte[headerLength];
                    fileStream.Read(headerBuffer, 0, headerLength);
                    var headerXml = Encoding.UTF8.GetString(headerBuffer);
                    
                    // Parse XML header
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(headerXml);
                    
                    // Extract metadata from XML
                    ExtractMetadataFromXml(xmlDoc, metadata);
                    ExtractPropertiesFromXml(xmlDoc, metadata);
                    ExtractImageInfoFromXml(xmlDoc, metadata);
                }
                
                return metadata;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse XISF metadata: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Parse XISF metadata from file and return comprehensive information
        /// </summary>
        public static Dictionary<string, object> ParseMetadata(byte[] buffer)
        {
            var metadata = new Dictionary<string, object>();
            
            try
            {
                // Validate XISF signature
                if (!IsValidXisfBuffer(buffer))
                {
                    throw new InvalidOperationException("Invalid XISF file signature");
                }
                
                // Read header
                var headerXml = ReadXisfHeader(buffer);
                
                // Parse XML header
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(headerXml);
                
                // Extract metadata from XML
                ExtractMetadataFromXml(xmlDoc, metadata);
                ExtractPropertiesFromXml(xmlDoc, metadata);
                ExtractImageInfoFromXml(xmlDoc, metadata);
                
                return metadata;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse XISF metadata: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Read XISF image data and return as byte array
        /// </summary>
        public static (int width, int height, byte[] pixels) ReadImage(byte[] buffer)
        {
            try
            {
                // Validate XISF signature
                if (!IsValidXisfBuffer(buffer))
                {
                    throw new InvalidOperationException("Invalid XISF file signature");
                }
                
                // Read and parse header
                var headerXml = ReadXisfHeader(buffer);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(headerXml);
                
                // Find the first Image element - handle both namespaced and non-namespaced XML
                XmlNode? imageNode = null;
                
                // First try namespace-aware approach
                if (xmlDoc.DocumentElement != null)
                {
                    // Try to find Image elements directly in the document
                    imageNode = FindImageElement(xmlDoc);
                }
                
                if (imageNode == null)
                {
                    // Provide better diagnostic information
                    var rootName = xmlDoc.DocumentElement?.Name ?? "null";
                    var childCount = xmlDoc.DocumentElement?.ChildNodes.Count ?? 0;
                    var availableElements = new List<string>();
                    
                    if (xmlDoc.DocumentElement?.ChildNodes != null)
                    {
                        foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                        {
                            if (node.NodeType == XmlNodeType.Element)
                            {
                                availableElements.Add(node.Name);
                            }
                        }
                    }
                    
                    throw new InvalidOperationException($"No Image element found in XISF file. Root element: '{rootName}', Child elements: [{string.Join(", ", availableElements)}]. Note: Image elements were detected but could not be accessed properly.");
                }
                
                // Extract image geometry
                var geometry = imageNode.Attributes?["geometry"]?.Value;
                if (string.IsNullOrEmpty(geometry))
                {
                    throw new InvalidOperationException("Missing geometry attribute in Image element");
                }
                
                var geometryParts = geometry.Split(':');
                if (geometryParts.Length < 3)
                {
                    throw new InvalidOperationException($"Invalid geometry format: {geometry}");
                }
                
                if (!int.TryParse(geometryParts[0], out int width) || 
                    !int.TryParse(geometryParts[1], out int height) ||
                    !int.TryParse(geometryParts[2], out int channels))
                {
                    throw new InvalidOperationException($"Invalid geometry values: {geometry}");
                }
                
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException($"Invalid image dimensions: {width}x{height}");
                }
                
                // Get sample format
                var sampleFormat = imageNode.Attributes?["sampleFormat"]?.Value ?? "UInt16";
                
                // Debug output to understand the file structure
                System.Diagnostics.Debug.WriteLine($"XISF Image Geometry: {width}x{height}, Channels: {channels}, Sample Format: {sampleFormat}");
                
                // Get location information
                var location = imageNode.Attributes?["location"]?.Value;
                if (string.IsNullOrEmpty(location))
                {
                    throw new InvalidOperationException("Missing location attribute in Image element");
                }
                
                // Parse attachment location (format: "attachment:position:size")
                if (!location.StartsWith("attachment:"))
                {
                    throw new NotSupportedException($"Unsupported location type: {location}. Only attachment locations are currently supported.");
                }
                
                var locationParts = location.Split(':');
                if (locationParts.Length != 3)
                {
                    throw new InvalidOperationException($"Invalid attachment location format: {location}");
                }
                
                if (!long.TryParse(locationParts[1], out long position) ||
                    !long.TryParse(locationParts[2], out long size))
                {
                    throw new InvalidOperationException($"Invalid attachment position/size: {location}");
                }
                
                // Validate buffer size
                if (buffer.Length < position + size)
                {
                    throw new ArgumentOutOfRangeException($"XISF buffer too small: expected at least {position + size} bytes, got {buffer.Length}");
                }
                
                // Read and convert image data
                var pixels = ReadImageData(buffer, (int)position, (int)size, width, height, channels, sampleFormat);
                
                return (width, height, pixels);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read XISF image: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Read XISF image data and return as RGB byte array for color display
        /// </summary>
        public static (int width, int height, byte[] rgbPixels) ReadImageRgb(byte[] buffer)
        {
            try
            {
                // Validate XISF signature
                if (!IsValidXisfBuffer(buffer))
                {
                    throw new InvalidOperationException("Invalid XISF file signature");
                }
                
                // Read and parse header
                var headerXml = ReadXisfHeader(buffer);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(headerXml);
                
                // Find the first Image element
                XmlNode? imageNode = FindImageElement(xmlDoc);
                if (imageNode == null)
                {
                    throw new InvalidOperationException("No Image element found in XISF file");
                }
                
                // Extract image geometry
                var geometry = imageNode.Attributes?["geometry"]?.Value;
                if (string.IsNullOrEmpty(geometry))
                {
                    throw new InvalidOperationException("Missing geometry attribute in Image element");
                }
                
                var geometryParts = geometry.Split(':');
                if (geometryParts.Length < 3)
                {
                    throw new InvalidOperationException($"Invalid geometry format: {geometry}");
                }
                
                if (!int.TryParse(geometryParts[0], out int width) || 
                    !int.TryParse(geometryParts[1], out int height) ||
                    !int.TryParse(geometryParts[2], out int channels))
                {
                    throw new InvalidOperationException($"Invalid geometry values: {geometry}");
                }
                
                // Get sample format
                var sampleFormat = imageNode.Attributes?["sampleFormat"]?.Value ?? "UInt16";
                
                // Get location information
                var location = imageNode.Attributes?["location"]?.Value;
                if (string.IsNullOrEmpty(location))
                {
                    throw new InvalidOperationException("Missing location attribute in Image element");
                }
                
                // Parse attachment location
                if (!location.StartsWith("attachment:"))
                {
                    throw new NotSupportedException($"Unsupported location type: {location}");
                }
                
                var locationParts = location.Split(':');
                if (locationParts.Length != 3)
                {
                    throw new InvalidOperationException($"Invalid attachment location format: {location}");
                }
                
                if (!long.TryParse(locationParts[1], out long position) ||
                    !long.TryParse(locationParts[2], out long size))
                {
                    throw new InvalidOperationException($"Invalid attachment position/size: {location}");
                }
                
                // Validate buffer size
                if (buffer.Length < position + size)
                {
                    throw new ArgumentOutOfRangeException($"XISF buffer too small: expected at least {position + size} bytes, got {buffer.Length}");
                }
                
                // Read and convert image data to RGB
                var rgbPixels = ReadImageDataRgb(buffer, (int)position, (int)size, width, height, channels, sampleFormat);
                
                return (width, height, rgbPixels);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read XISF RGB image: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Find Image element in XISF XML document, handling namespaces properly
        /// </summary>
        private static XmlNode? FindImageElement(XmlDocument xmlDoc)
        {
            if (xmlDoc.DocumentElement == null)
                return null;
                
            // First try simple XPath (for non-namespaced XML)
            var imageNode = xmlDoc.SelectSingleNode("//Image");
            if (imageNode != null)
            {
                // Debug: Check if there are multiple Image elements
                var allImages = xmlDoc.SelectNodes("//Image");
                if (allImages != null && allImages.Count > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"XISF Warning: Found {allImages.Count} Image elements, using the first one");
                    for (int i = 0; i < allImages.Count; i++)
                    {
                        var img = allImages[i];
                        var geo = img?.Attributes?["geometry"]?.Value ?? "unknown";
                        var loc = img?.Attributes?["location"]?.Value ?? "unknown";
                        System.Diagnostics.Debug.WriteLine($"  Image {i}: geometry={geo}, location={loc}");
                    }
                }
                return imageNode;
            }
                
            // If that fails, try to find Image elements by iterating through children
            // This handles cases where namespaces might interfere with XPath
            var foundImage = FindImageElementRecursive(xmlDoc.DocumentElement);
            if (foundImage != null)
            {
                System.Diagnostics.Debug.WriteLine("XISF: Found Image element via recursive search");
            }
            return foundImage;
        }
        
        /// <summary>
        /// Recursively search for Image element
        /// </summary>
        private static XmlNode? FindImageElementRecursive(XmlNode node)
        {
            // Check if current node is an Image element (ignore namespace)
            if (node.LocalName == "Image" && node.NodeType == XmlNodeType.Element)
                return node;
                
            // Search children
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    var result = FindImageElementRecursive(child);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Find element by local name (ignoring namespace)
        /// </summary>
        private static XmlNode? FindElementByLocalName(XmlDocument xmlDoc, string localName)
        {
            if (xmlDoc.DocumentElement == null)
                return null;
                
            return FindElementByLocalNameRecursive(xmlDoc.DocumentElement, localName);
        }
        
        /// <summary>
        /// Recursively find element by local name
        /// </summary>
        private static XmlNode? FindElementByLocalNameRecursive(XmlNode node, string localName)
        {
            if (node.LocalName == localName && node.NodeType == XmlNodeType.Element)
                return node;
                
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    var result = FindElementByLocalNameRecursive(child, localName);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Find child elements by local name
        /// </summary>
        private static List<XmlNode> FindChildElementsByLocalName(XmlNode parent, string localName)
        {
            var results = new List<XmlNode>();
            
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element && child.LocalName == localName)
                {
                    results.Add(child);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Find all elements by local name throughout the document
        /// </summary>
        private static List<XmlNode> FindAllElementsByLocalName(XmlDocument xmlDoc, string localName)
        {
            var results = new List<XmlNode>();
            
            if (xmlDoc.DocumentElement != null)
            {
                FindAllElementsByLocalNameRecursive(xmlDoc.DocumentElement, localName, results);
            }
            
            return results;
        }
        
        /// <summary>
        /// Recursively find all elements by local name
        /// </summary>
        private static void FindAllElementsByLocalNameRecursive(XmlNode node, string localName, List<XmlNode> results)
        {
            if (node.LocalName == localName && node.NodeType == XmlNodeType.Element)
            {
                results.Add(node);
            }
            
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    FindAllElementsByLocalNameRecursive(child, localName, results);
                }
            }
        }
        
        /// <summary>
        /// Check if buffer contains valid XISF data
        /// </summary>
        private static bool IsValidXisfBuffer(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 16)
                return false;
                
            var signature = Encoding.ASCII.GetString(buffer, 0, 8);
            return signature == XISF_SIGNATURE;
        }
        
        /// <summary>
        /// Read XISF header from buffer
        /// </summary>
        private static string ReadXisfHeader(byte[] buffer)
        {
            // Read header length (bytes 8-11, little-endian)
            int headerLength = BitConverter.ToInt32(buffer, 8);
            
            if (headerLength <= 0 || headerLength > buffer.Length - 16)
            {
                throw new InvalidOperationException($"Invalid header length: {headerLength}. File size: {buffer.Length} bytes");
            }
            
            // Skip signature (8 bytes), header length (4 bytes), and reserved field (4 bytes)
            int headerStart = 16;
            
            // Ensure we don't read beyond buffer
            if (headerStart + headerLength > buffer.Length)
            {
                throw new InvalidOperationException($"Header extends beyond file boundary. Header start: {headerStart}, Header length: {headerLength}, File size: {buffer.Length}");
            }
            
            // Read header as UTF-8 XML
            var headerXml = Encoding.UTF8.GetString(buffer, headerStart, headerLength);
            
            // Basic XML validation - ensure it's not empty or malformed
            if (string.IsNullOrWhiteSpace(headerXml))
            {
                throw new InvalidOperationException("XISF header is empty");
            }
            
            if (!headerXml.TrimStart().StartsWith("<"))
            {
                throw new InvalidOperationException("XISF header does not appear to be valid XML");
            }
            
            return headerXml;
        }
        
        /// <summary>
        /// Extract metadata properties from XISF XML header
        /// </summary>
        private static void ExtractMetadataFromXml(XmlDocument xmlDoc, Dictionary<string, object> metadata)
        {
            // Look for Metadata element and its properties - handle namespaces
            var metadataNode = FindElementByLocalName(xmlDoc, "Metadata");
            if (metadataNode != null)
            {
                var propertyNodes = FindChildElementsByLocalName(metadataNode, "Property");
                foreach (XmlNode propertyNode in propertyNodes)
                {
                    var id = propertyNode.Attributes?["id"]?.Value;
                    var type = propertyNode.Attributes?["type"]?.Value;
                    var value = propertyNode.Attributes?["value"]?.Value ?? propertyNode.InnerText;
                    
                    if (!string.IsNullOrEmpty(id))
                    {
                        metadata[$"Metadata_{id}"] = ParseXisfValue(value, type);
                    }
                }
            }
        }
        
        /// <summary>
        /// Extract all properties from XISF XML header
        /// </summary>
        private static void ExtractPropertiesFromXml(XmlDocument xmlDoc, Dictionary<string, object> metadata)
        {
            // Get all Property elements using namespace-aware search
            var propertyNodes = FindAllElementsByLocalName(xmlDoc, "Property");
            foreach (XmlNode propertyNode in propertyNodes)
            {
                var id = propertyNode.Attributes?["id"]?.Value;
                var type = propertyNode.Attributes?["type"]?.Value;
                var value = propertyNode.Attributes?["value"]?.Value ?? propertyNode.InnerText;
                var comment = propertyNode.Attributes?["comment"]?.Value;
                
                if (!string.IsNullOrEmpty(id))
                {
                    var key = id.Replace(":", "_"); // Replace namespace separator for display
                    metadata[key] = ParseXisfValue(value, type);
                    
                    if (!string.IsNullOrEmpty(comment))
                    {
                        metadata[$"{key}_Comment"] = comment;
                    }
                }
            }
        }
        
        /// <summary>
        /// Extract image information from XISF XML header
        /// </summary>
        private static void ExtractImageInfoFromXml(XmlDocument xmlDoc, Dictionary<string, object> metadata)
        {
            var imageNode = FindImageElement(xmlDoc);
            if (imageNode?.Attributes != null)
            {
                foreach (XmlAttribute attr in imageNode.Attributes)
                {
                    var key = $"Image_{attr.Name}";
                    metadata[key] = attr.Value;
                }
                
                // Parse geometry for easier access
                var geometry = imageNode.Attributes["geometry"]?.Value;
                if (!string.IsNullOrEmpty(geometry))
                {
                    var parts = geometry.Split(':');
                    if (parts.Length >= 3)
                    {
                        if (int.TryParse(parts[0], out int width))
                            metadata["Image_Width"] = width;
                        if (int.TryParse(parts[1], out int height))
                            metadata["Image_Height"] = height;
                        if (int.TryParse(parts[2], out int channels))
                            metadata["Image_Channels"] = channels;
                    }
                }
                
                // Parse bounds for display range
                var bounds = imageNode.Attributes["bounds"]?.Value;
                if (!string.IsNullOrEmpty(bounds))
                {
                    var parts = bounds.Split(':');
                    if (parts.Length >= 2)
                    {
                        if (double.TryParse(parts[0], out double lower))
                            metadata["Image_LowerBound"] = lower;
                        if (double.TryParse(parts[1], out double upper))
                            metadata["Image_UpperBound"] = upper;
                    }
                }
            }
            
            // Extract FITS keywords if present
            var fitsKeywords = FindAllElementsByLocalName(xmlDoc, "FITSKeyword");
            foreach (XmlNode fitsNode in fitsKeywords)
            {
                var name = fitsNode.Attributes?["name"]?.Value;
                var value = fitsNode.Attributes?["value"]?.Value;
                var comment = fitsNode.Attributes?["comment"]?.Value;
                
                if (!string.IsNullOrEmpty(name))
                {
                    metadata[$"FITS_{name}"] = value ?? "";
                    if (!string.IsNullOrEmpty(comment))
                    {
                        metadata[$"FITS_{name}_Comment"] = comment;
                    }
                }
            }
        }
        
        /// <summary>
        /// Parse XISF property value based on type
        /// </summary>
        private static object ParseXisfValue(string value, string? type)
        {
            if (string.IsNullOrEmpty(value))
                return "";
                
            return type?.ToLowerInvariant() switch
            {
                "boolean" => value.Equals("1") || value.Equals("true", StringComparison.OrdinalIgnoreCase),
                "int8" or "int16" or "int32" or "int" => int.TryParse(value, out int intVal) ? intVal : value,
                "uint8" or "uint16" or "uint32" or "uint" => uint.TryParse(value, out uint uintVal) ? uintVal : value,
                "int64" => long.TryParse(value, out long longVal) ? longVal : value,
                "uint64" => ulong.TryParse(value, out ulong ulongVal) ? ulongVal : value,
                "float32" or "float" => float.TryParse(value, out float floatVal) ? floatVal : value,
                "float64" or "double" => double.TryParse(value, out double doubleVal) ? doubleVal : value,
                "timepoint" => DateTime.TryParse(value, out DateTime dateVal) ? dateVal : value,
                "string" or null => value,
                _ => value
            };
        }
        
        /// <summary>
        /// Read and convert XISF image data to byte array
        /// </summary>
        private static byte[] ReadImageData(byte[] buffer, int position, int size, int width, int height, int channels, string sampleFormat)
        {
            // For multi-channel images, we need to be more careful about data layout
            // Note: XISF uses little-endian by default (unlike FITS which uses big-endian)
            
            System.Diagnostics.Debug.WriteLine($"XISF ReadImageData: {width}x{height}, Channels: {channels}, Format: {sampleFormat}, Position: {position}, Size: {size}");
            
            int totalPixels = width * height;
            var pixels = new byte[totalPixels];
            
            switch (sampleFormat.ToUpperInvariant())
            {
                case "UINT8":
                    ConvertUInt8Data(buffer, position, pixels, totalPixels, channels);
                    break;
                case "UINT16":
                    ConvertUInt16Data(buffer, position, pixels, totalPixels, channels);
                    break;
                case "UINT32":
                    ConvertUInt32Data(buffer, position, pixels, totalPixels, channels);
                    break;
                case "FLOAT32":
                    ConvertFloat32Data(buffer, position, pixels, totalPixels, channels);
                    break;
                case "FLOAT64":
                    ConvertFloat64Data(buffer, position, pixels, totalPixels, channels);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported sample format: {sampleFormat}");
            }
            
            return pixels;
        }
        
        /// <summary>
        /// Read XISF image data and return as RGB byte array
        /// </summary>
        private static byte[] ReadImageDataRgb(byte[] buffer, int position, int size, int width, int height, int channels, string sampleFormat)
        {
            System.Diagnostics.Debug.WriteLine($"XISF ReadImageDataRgb: {width}x{height}, Channels: {channels}, Format: {sampleFormat}, Position: {position}, Size: {size}");
            
            int totalPixels = width * height;
            byte[] rgbPixels;
            
            switch (sampleFormat.ToUpperInvariant())
            {
                case "UINT8":
                    rgbPixels = ConvertUInt8DataRgb(buffer, position, totalPixels, channels);
                    break;
                case "UINT16":
                    rgbPixels = ConvertUInt16DataRgb(buffer, position, totalPixels, channels);
                    break;
                case "UINT32":
                    rgbPixels = ConvertUInt32DataRgb(buffer, position, totalPixels, channels);
                    break;
                case "FLOAT32":
                    rgbPixels = ConvertFloat32DataRgb(buffer, position, totalPixels, channels);
                    break;
                case "FLOAT64":
                    rgbPixels = ConvertFloat64DataRgb(buffer, position, totalPixels, channels);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported sample format: {sampleFormat}");
            }
            
            return rgbPixels;
        }
        
        /// <summary>
        /// Convert UInt8 XISF data to byte array
        /// </summary>
        private static void ConvertUInt8Data(byte[] buffer, int offset, byte[] pixels, int totalPixels, int channels)
        {
            System.Diagnostics.Debug.WriteLine($"ConvertUInt8Data: totalPixels={totalPixels}, channels={channels}, offset={offset}");
            
            if (channels == 1)
            {
                // Single channel - direct copy
                for (int i = 0; i < totalPixels && offset + i < buffer.Length; i++)
                {
                    pixels[i] = buffer[offset + i];
                }
            }
            else
            {
                // Multi-channel - assume channel-planar layout
                int pixelsPerChannel = totalPixels;
                
                for (int i = 0; i < totalPixels; i++)
                {
                    int sum = 0;
                    int validChannels = 0;
                    
                    for (int c = 0; c < channels; c++)
                    {
                        int channelOffset = offset + c * pixelsPerChannel + i;
                        if (channelOffset < buffer.Length)
                        {
                            sum += buffer[channelOffset];
                            validChannels++;
                        }
                    }
                    
                    if (validChannels > 0)
                    {
                        pixels[i] = (byte)(sum / validChannels);
                    }
                }
            }
        }
        
        /// <summary>
        /// Convert UInt16 XISF data to byte array with auto-scaling
        /// </summary>
        private static void ConvertUInt16Data(byte[] buffer, int offset, byte[] pixels, int totalPixels, int channels)
        {
            var values = new double[totalPixels];
            double min = double.MaxValue, max = double.MinValue;
            
            System.Diagnostics.Debug.WriteLine($"ConvertUInt16Data: totalPixels={totalPixels}, channels={channels}, offset={offset}");
            
            // For multi-channel images, we need to determine the data layout
            // Most XISF files use channel-planar layout: all R values, then all G values, then all B values
            if (channels == 1)
            {
                // Single channel - straightforward
                for (int i = 0; i < totalPixels && offset + i * 2 + 1 < buffer.Length; i++)
                {
                    ushort value = (ushort)(buffer[offset + i * 2] | (buffer[offset + i * 2 + 1] << 8));
                    values[i] = value;
                    if (values[i] < min) min = values[i];
                    if (values[i] > max) max = values[i];
                }
            }
            else
            {
                // Multi-channel - try channel-planar layout first (most common in XISF)
                // Layout: R1,R2,R3...Rn,G1,G2,G3...Gn,B1,B2,B3...Bn
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 2; // 2 bytes per UInt16
                
                for (int i = 0; i < totalPixels; i++)
                {
                    double sum = 0;
                    int validChannels = 0;
                    
                    for (int c = 0; c < channels; c++)
                    {
                        int channelOffset = offset + c * bytesPerChannel + i * 2;
                        if (channelOffset + 1 < buffer.Length)
                        {
                            ushort value = (ushort)(buffer[channelOffset] | (buffer[channelOffset + 1] << 8));
                            sum += value;
                            validChannels++;
                        }
                    }
                    
                    if (validChannels > 0)
                    {
                        values[i] = sum / validChannels;
                        if (values[i] < min) min = values[i];
                        if (values[i] > max) max = values[i];
                    }
                }
            }
            
            // Scale to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(values[0], 0, 255);
                Array.Fill(pixels, constValue);
            }
        }
        
        /// <summary>
        /// Convert UInt32 XISF data to byte array with auto-scaling
        /// </summary>
        private static void ConvertUInt32Data(byte[] buffer, int offset, byte[] pixels, int totalPixels, int channels)
        {
            var values = new double[totalPixels];
            double min = double.MaxValue, max = double.MinValue;
            int stride = channels * 4; // 4 bytes per UInt32
            
            // Read all values and find min/max
            for (int i = 0; i < totalPixels && offset + i * stride + 3 < buffer.Length; i++)
            {
                if (channels == 1)
                {
                    // XISF uses little-endian
                    uint value = (uint)(buffer[offset + i * stride] | 
                                       (buffer[offset + i * stride + 1] << 8) |
                                       (buffer[offset + i * stride + 2] << 16) |
                                       (buffer[offset + i * stride + 3] << 24));
                    values[i] = value;
                }
                else
                {
                    // Average channels
                    double sum = 0;
                    for (int c = 0; c < channels && offset + i * stride + c * 4 + 3 < buffer.Length; c++)
                    {
                        uint value = (uint)(buffer[offset + i * stride + c * 4] | 
                                           (buffer[offset + i * stride + c * 4 + 1] << 8) |
                                           (buffer[offset + i * stride + c * 4 + 2] << 16) |
                                           (buffer[offset + i * stride + c * 4 + 3] << 24));
                        sum += value;
                    }
                    values[i] = sum / channels;
                }
                
                if (values[i] < min) min = values[i];
                if (values[i] > max) max = values[i];
            }
            
            // Scale to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(values[0], 0, 255);
                Array.Fill(pixels, constValue);
            }
        }
        
        /// <summary>
        /// Convert Float32 XISF data to byte array with auto-scaling
        /// </summary>
        private static void ConvertFloat32Data(byte[] buffer, int offset, byte[] pixels, int totalPixels, int channels)
        {
            var values = new double[totalPixels];
            double min = double.MaxValue, max = double.MinValue;
            
            System.Diagnostics.Debug.WriteLine($"ConvertFloat32Data: totalPixels={totalPixels}, channels={channels}, offset={offset}");
            
            // For multi-channel images, assume channel-planar layout: R1,R2,R3...Rn,G1,G2,G3...Gn,B1,B2,B3...Bn
            if (channels == 1)
            {
                // Single channel - straightforward
                for (int i = 0; i < totalPixels && offset + i * 4 + 3 < buffer.Length; i++)
                {
                    // XISF uses little-endian - can use BitConverter.ToSingle directly
                    float value = BitConverter.ToSingle(buffer, offset + i * 4);
                    values[i] = float.IsFinite(value) ? value : 0;
                    if (values[i] < min) min = values[i];
                    if (values[i] > max) max = values[i];
                }
            }
            else
            {
                // Multi-channel - use channel-planar layout
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 4; // 4 bytes per Float32
                
                // Read a few sample values from each channel for debugging
                if (totalPixels > 0)
                {
                    for (int c = 0; c < Math.Min(channels, 3); c++)
                    {
                        int sampleOffset = offset + c * bytesPerChannel;
                        if (sampleOffset + 3 < buffer.Length)
                        {
                            float sampleValue = BitConverter.ToSingle(buffer, sampleOffset);
                            System.Diagnostics.Debug.WriteLine($"  Channel {c} sample[0]: {sampleValue}");
                        }
                    }
                }
                
                for (int i = 0; i < totalPixels; i++)
                {
                    double rValue = 0, gValue = 0, bValue = 0;
                    int validChannels = 0;
                    
                    // Read each channel
                    for (int c = 0; c < channels; c++)
                    {
                        int channelOffset = offset + c * bytesPerChannel + i * 4;
                        if (channelOffset + 3 < buffer.Length)
                        {
                            float value = BitConverter.ToSingle(buffer, channelOffset);
                            if (float.IsFinite(value))
                            {
                                if (c == 0) rValue = value;      // Red channel
                                else if (c == 1) gValue = value; // Green channel  
                                else if (c == 2) bValue = value; // Blue channel
                                validChannels++;
                            }
                        }
                    }
                    
                    if (validChannels > 0)
                    {
                        // Use luminance-weighted conversion for better grayscale (ITU-R BT.709)
                        // This gives more weight to green (which the human eye is most sensitive to)
                        if (channels >= 3)
                        {
                            values[i] = 0.2126 * rValue + 0.7152 * gValue + 0.0722 * bValue;
                        }
                        else
                        {
                            // For non-RGB channels, just average
                            values[i] = (rValue + gValue + bValue) / validChannels;
                        }
                        
                        if (values[i] < min) min = values[i];
                        if (values[i] > max) max = values[i];
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"  RGB Float32 - Min: {min:F6}, Max: {max:F6}, Range: {max-min:F6}");
            }
            
            // Scale to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(min, 0, 255);
                Array.Fill(pixels, constValue);
            }
        }
        
        /// <summary>
        /// Convert Float32 XISF data to RGB byte array (3 bytes per pixel: R,G,B)
        /// </summary>
        private static byte[] ConvertFloat32DataRgb(byte[] buffer, int offset, int totalPixels, int channels)
        {
            System.Diagnostics.Debug.WriteLine($"ConvertFloat32DataRgb: totalPixels={totalPixels}, channels={channels}, offset={offset}");
            
            byte[] rgbPixels = new byte[totalPixels * 3]; // 3 bytes per pixel (R,G,B)
            
            if (channels == 1)
            {
                // Single channel - create grayscale RGB
                var values = new double[totalPixels];
                double min = double.MaxValue, max = double.MinValue;
                
                for (int i = 0; i < totalPixels && offset + i * 4 + 3 < buffer.Length; i++)
                {
                    float value = BitConverter.ToSingle(buffer, offset + i * 4);
                    values[i] = float.IsFinite(value) ? value : 0;
                    if (values[i] < min) min = values[i];
                    if (values[i] > max) max = values[i];
                }
                
                // Scale to RGB
                double range = max - min;
                if (range > 0)
                {
                    for (int i = 0; i < totalPixels; i++)
                    {
                        int scaled = (int)((values[i] - min) * 255.0 / range);
                        byte gray = (byte)Math.Clamp(scaled, 0, 255);
                        rgbPixels[i * 3] = gray;     // R
                        rgbPixels[i * 3 + 1] = gray; // G
                        rgbPixels[i * 3 + 2] = gray; // B
                    }
                }
                else
                {
                    byte constValue = (byte)Math.Clamp(min, 0, 255);
                    for (int i = 0; i < totalPixels; i++)
                    {
                        rgbPixels[i * 3] = constValue;     // R
                        rgbPixels[i * 3 + 1] = constValue; // G
                        rgbPixels[i * 3 + 2] = constValue; // B
                    }
                }
            }
            else if (channels >= 3)
            {
                // Multi-channel RGB - handle channel-planar layout
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 4; // 4 bytes per Float32
                
                var rValues = new double[totalPixels];
                var gValues = new double[totalPixels];
                var bValues = new double[totalPixels];
                
                double rMin = double.MaxValue, rMax = double.MinValue;
                double gMin = double.MaxValue, gMax = double.MinValue;
                double bMin = double.MaxValue, bMax = double.MinValue;
                
                // Read RGB channels separately
                for (int i = 0; i < totalPixels; i++)
                {
                    // Red channel (channel 0)
                    int rOffset = offset + i * 4;
                    if (rOffset + 3 < buffer.Length)
                    {
                        float rValue = BitConverter.ToSingle(buffer, rOffset);
                        rValues[i] = float.IsFinite(rValue) ? rValue : 0;
                        if (rValues[i] < rMin) rMin = rValues[i];
                        if (rValues[i] > rMax) rMax = rValues[i];
                    }
                    
                    // Green channel (channel 1)
                    int gOffset = offset + bytesPerChannel + i * 4;
                    if (gOffset + 3 < buffer.Length)
                    {
                        float gValue = BitConverter.ToSingle(buffer, gOffset);
                        gValues[i] = float.IsFinite(gValue) ? gValue : 0;
                        if (gValues[i] < gMin) gMin = gValues[i];
                        if (gValues[i] > gMax) gMax = gValues[i];
                    }
                    
                    // Blue channel (channel 2)
                    int bOffset = offset + 2 * bytesPerChannel + i * 4;
                    if (bOffset + 3 < buffer.Length)
                    {
                        float bValue = BitConverter.ToSingle(buffer, bOffset);
                        bValues[i] = float.IsFinite(bValue) ? bValue : 0;
                        if (bValues[i] < bMin) bMin = bValues[i];
                        if (bValues[i] > bMax) bMax = bValues[i];
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"  RGB ranges - R: {rMin:F6} to {rMax:F6}, G: {gMin:F6} to {gMax:F6}, B: {bMin:F6} to {bMax:F6}");
                
                // Scale each channel independently to 0-255
                double rRange = rMax - rMin;
                double gRange = gMax - gMin;
                double bRange = bMax - bMin;
                
                for (int i = 0; i < totalPixels; i++)
                {
                    byte r = 0, g = 0, b = 0;
                    
                    if (rRange > 0)
                        r = (byte)Math.Clamp((int)((rValues[i] - rMin) * 255.0 / rRange), 0, 255);
                    else
                        r = (byte)Math.Clamp(rMin, 0, 255);
                        
                    if (gRange > 0)
                        g = (byte)Math.Clamp((int)((gValues[i] - gMin) * 255.0 / gRange), 0, 255);
                    else
                        g = (byte)Math.Clamp(gMin, 0, 255);
                        
                    if (bRange > 0)
                        b = (byte)Math.Clamp((int)((bValues[i] - bMin) * 255.0 / bRange), 0, 255);
                    else
                        b = (byte)Math.Clamp(bMin, 0, 255);
                    
                    rgbPixels[i * 3] = r;
                    rgbPixels[i * 3 + 1] = g;
                    rgbPixels[i * 3 + 2] = b;
                }
            }
            else
            {
                // Handle 2-channel or other cases - create grayscale
                Array.Fill(rgbPixels, (byte)128);
            }
            
            return rgbPixels;
        }
        
        /// <summary>
        /// Convert UInt8 XISF data to RGB byte array (3 bytes per pixel: R,G,B)
        /// </summary>
        private static byte[] ConvertUInt8DataRgb(byte[] buffer, int offset, int totalPixels, int channels)
        {
            byte[] rgbPixels = new byte[totalPixels * 3];
            
            if (channels == 1)
            {
                for (int i = 0; i < totalPixels && offset + i < buffer.Length; i++)
                {
                    byte gray = buffer[offset + i];
                    rgbPixels[i * 3] = gray;
                    rgbPixels[i * 3 + 1] = gray;
                    rgbPixels[i * 3 + 2] = gray;
                }
            }
            else if (channels >= 3)
            {
                int pixelsPerChannel = totalPixels;
                for (int i = 0; i < totalPixels; i++)
                {
                    byte r = (offset + i < buffer.Length) ? buffer[offset + i] : (byte)0;
                    byte g = (offset + pixelsPerChannel + i < buffer.Length) ? buffer[offset + pixelsPerChannel + i] : (byte)0;
                    byte b = (offset + 2 * pixelsPerChannel + i < buffer.Length) ? buffer[offset + 2 * pixelsPerChannel + i] : (byte)0;
                    
                    rgbPixels[i * 3] = r;
                    rgbPixels[i * 3 + 1] = g;
                    rgbPixels[i * 3 + 2] = b;
                }
            }
            
            return rgbPixels;
        }
        
        /// <summary>
        /// Convert UInt16 XISF data to RGB byte array (3 bytes per pixel: R,G,B)
        /// </summary>
        private static byte[] ConvertUInt16DataRgb(byte[] buffer, int offset, int totalPixels, int channels)
        {
            byte[] rgbPixels = new byte[totalPixels * 3];
            
            if (channels == 1)
            {
                for (int i = 0; i < totalPixels && offset + i * 2 + 1 < buffer.Length; i++)
                {
                    ushort value = BitConverter.ToUInt16(buffer, offset + i * 2);
                    byte gray = (byte)(value >> 8); // Scale from 16-bit to 8-bit
                    rgbPixels[i * 3] = gray;
                    rgbPixels[i * 3 + 1] = gray;
                    rgbPixels[i * 3 + 2] = gray;
                }
            }
            else if (channels >= 3)
            {
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 2;
                
                for (int i = 0; i < totalPixels; i++)
                {
                    ushort rValue = (offset + i * 2 + 1 < buffer.Length) ? BitConverter.ToUInt16(buffer, offset + i * 2) : (ushort)0;
                    ushort gValue = (offset + bytesPerChannel + i * 2 + 1 < buffer.Length) ? BitConverter.ToUInt16(buffer, offset + bytesPerChannel + i * 2) : (ushort)0;
                    ushort bValue = (offset + 2 * bytesPerChannel + i * 2 + 1 < buffer.Length) ? BitConverter.ToUInt16(buffer, offset + 2 * bytesPerChannel + i * 2) : (ushort)0;
                    
                    rgbPixels[i * 3] = (byte)(rValue >> 8);
                    rgbPixels[i * 3 + 1] = (byte)(gValue >> 8);
                    rgbPixels[i * 3 + 2] = (byte)(bValue >> 8);
                }
            }
            
            return rgbPixels;
        }
        
        /// <summary>
        /// Convert UInt32 XISF data to RGB byte array (3 bytes per pixel: R,G,B)
        /// </summary>
        private static byte[] ConvertUInt32DataRgb(byte[] buffer, int offset, int totalPixels, int channels)
        {
            byte[] rgbPixels = new byte[totalPixels * 3];
            
            if (channels == 1)
            {
                for (int i = 0; i < totalPixels && offset + i * 4 + 3 < buffer.Length; i++)
                {
                    uint value = BitConverter.ToUInt32(buffer, offset + i * 4);
                    byte gray = (byte)(value >> 24); // Scale from 32-bit to 8-bit
                    rgbPixels[i * 3] = gray;
                    rgbPixels[i * 3 + 1] = gray;
                    rgbPixels[i * 3 + 2] = gray;
                }
            }
            else if (channels >= 3)
            {
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 4;
                
                for (int i = 0; i < totalPixels; i++)
                {
                    uint rValue = (offset + i * 4 + 3 < buffer.Length) ? BitConverter.ToUInt32(buffer, offset + i * 4) : 0u;
                    uint gValue = (offset + bytesPerChannel + i * 4 + 3 < buffer.Length) ? BitConverter.ToUInt32(buffer, offset + bytesPerChannel + i * 4) : 0u;
                    uint bValue = (offset + 2 * bytesPerChannel + i * 4 + 3 < buffer.Length) ? BitConverter.ToUInt32(buffer, offset + 2 * bytesPerChannel + i * 4) : 0u;
                    
                    rgbPixels[i * 3] = (byte)(rValue >> 24);
                    rgbPixels[i * 3 + 1] = (byte)(gValue >> 24);
                    rgbPixels[i * 3 + 2] = (byte)(bValue >> 24);
                }
            }
            
            return rgbPixels;
        }
        
        /// <summary>
        /// Convert Float64 XISF data to RGB byte array (3 bytes per pixel: R,G,B)
        /// </summary>
        private static byte[] ConvertFloat64DataRgb(byte[] buffer, int offset, int totalPixels, int channels)
        {
            byte[] rgbPixels = new byte[totalPixels * 3];
            
            if (channels == 1)
            {
                var values = new double[totalPixels];
                double min = double.MaxValue, max = double.MinValue;
                
                for (int i = 0; i < totalPixels && offset + i * 8 + 7 < buffer.Length; i++)
                {
                    double value = BitConverter.ToDouble(buffer, offset + i * 8);
                    values[i] = double.IsFinite(value) ? value : 0;
                    if (values[i] < min) min = values[i];
                    if (values[i] > max) max = values[i];
                }
                
                double range = max - min;
                if (range > 0)
                {
                    for (int i = 0; i < totalPixels; i++)
                    {
                        int scaled = (int)((values[i] - min) * 255.0 / range);
                        byte gray = (byte)Math.Clamp(scaled, 0, 255);
                        rgbPixels[i * 3] = gray;
                        rgbPixels[i * 3 + 1] = gray;
                        rgbPixels[i * 3 + 2] = gray;
                    }
                }
            }
            else if (channels >= 3)
            {
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 8;
                
                var rValues = new double[totalPixels];
                var gValues = new double[totalPixels];
                var bValues = new double[totalPixels];
                
                double rMin = double.MaxValue, rMax = double.MinValue;
                double gMin = double.MaxValue, gMax = double.MinValue;
                double bMin = double.MaxValue, bMax = double.MinValue;
                
                for (int i = 0; i < totalPixels; i++)
                {
                    int rOffset = offset + i * 8;
                    int gOffset = offset + bytesPerChannel + i * 8;
                    int bOffset = offset + 2 * bytesPerChannel + i * 8;
                    
                    if (rOffset + 7 < buffer.Length)
                    {
                        double rValue = BitConverter.ToDouble(buffer, rOffset);
                        rValues[i] = double.IsFinite(rValue) ? rValue : 0;
                        if (rValues[i] < rMin) rMin = rValues[i];
                        if (rValues[i] > rMax) rMax = rValues[i];
                    }
                    
                    if (gOffset + 7 < buffer.Length)
                    {
                        double gValue = BitConverter.ToDouble(buffer, gOffset);
                        gValues[i] = double.IsFinite(gValue) ? gValue : 0;
                        if (gValues[i] < gMin) gMin = gValues[i];
                        if (gValues[i] > gMax) gMax = gValues[i];
                    }
                    
                    if (bOffset + 7 < buffer.Length)
                    {
                        double bValue = BitConverter.ToDouble(buffer, bOffset);
                        bValues[i] = double.IsFinite(bValue) ? bValue : 0;
                        if (bValues[i] < bMin) bMin = bValues[i];
                        if (bValues[i] > bMax) bMax = bValues[i];
                    }
                }
                
                double rRange = rMax - rMin;
                double gRange = gMax - gMin;
                double bRange = bMax - bMin;
                
                for (int i = 0; i < totalPixels; i++)
                {
                    byte r = (rRange > 0) ? (byte)Math.Clamp((int)((rValues[i] - rMin) * 255.0 / rRange), 0, 255) : (byte)Math.Clamp(rMin, 0, 255);
                    byte g = (gRange > 0) ? (byte)Math.Clamp((int)((gValues[i] - gMin) * 255.0 / gRange), 0, 255) : (byte)Math.Clamp(gMin, 0, 255);
                    byte b = (bRange > 0) ? (byte)Math.Clamp((int)((bValues[i] - bMin) * 255.0 / bRange), 0, 255) : (byte)Math.Clamp(bMin, 0, 255);
                    
                    rgbPixels[i * 3] = r;
                    rgbPixels[i * 3 + 1] = g;
                    rgbPixels[i * 3 + 2] = b;
                }
            }
            
            return rgbPixels;
        }
        
        /// <summary>
        /// Convert Float64 XISF data to byte array with auto-scaling
        /// </summary>
        private static void ConvertFloat64Data(byte[] buffer, int offset, byte[] pixels, int totalPixels, int channels)
        {
            var values = new double[totalPixels];
            double min = double.MaxValue, max = double.MinValue;
            
            System.Diagnostics.Debug.WriteLine($"ConvertFloat64Data: totalPixels={totalPixels}, channels={channels}, offset={offset}");
            
            // For multi-channel images, assume channel-planar layout
            if (channels == 1)
            {
                // Single channel - straightforward
                for (int i = 0; i < totalPixels && offset + i * 8 + 7 < buffer.Length; i++)
                {
                    double value = BitConverter.ToDouble(buffer, offset + i * 8);
                    values[i] = double.IsFinite(value) ? value : 0;
                    if (values[i] < min) min = values[i];
                    if (values[i] > max) max = values[i];
                }
            }
            else
            {
                // Multi-channel - use channel-planar layout
                int pixelsPerChannel = totalPixels;
                int bytesPerChannel = pixelsPerChannel * 8; // 8 bytes per Float64
                
                for (int i = 0; i < totalPixels; i++)
                {
                    double sum = 0;
                    int validChannels = 0;
                    
                    for (int c = 0; c < channels; c++)
                    {
                        int channelOffset = offset + c * bytesPerChannel + i * 8;
                        if (channelOffset + 7 < buffer.Length)
                        {
                            double value = BitConverter.ToDouble(buffer, channelOffset);
                            if (double.IsFinite(value))
                            {
                                sum += value;
                                validChannels++;
                            }
                        }
                    }
                    
                    if (validChannels > 0)
                    {
                        values[i] = sum / validChannels;
                        if (values[i] < min) min = values[i];
                        if (values[i] > max) max = values[i];
                    }
                }
            }
            
            // Scale to 0-255 range
            double range = max - min;
            if (range > 0)
            {
                for (int i = 0; i < totalPixels; i++)
                {
                    int scaled = (int)((values[i] - min) * 255.0 / range);
                    pixels[i] = (byte)Math.Clamp(scaled, 0, 255);
                }
            }
            else
            {
                byte constValue = (byte)Math.Clamp(min, 0, 255);
                Array.Fill(pixels, constValue);
            }
        }
    }
}