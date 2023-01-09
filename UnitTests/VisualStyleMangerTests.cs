﻿using System;
using System.Linq;
using System.Text.Json;
using Xunit;


namespace Terminal.Gui.Core {
	public class VisualStyleMangerTests {

		public static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions () {
			Converters = {
				new AttributeJsonConverter (),
				new ColorJsonConverter ()
				}
		};

		public VisualStyleMangerTests ()
		{
		}

		[Fact, AutoInitShutdown]
		public void TestColorSchemeRoundTrip ()
		{
			var serializedColors = JsonSerializer.Serialize (Colors.Base, _jsonOptions);
			var deserializedColors = JsonSerializer.Deserialize<ColorScheme> (serializedColors, _jsonOptions);

			Assert.Equal (Colors.Base.Normal, deserializedColors.Normal);
			Assert.Equal (Colors.Base.Focus, deserializedColors.Focus);
			Assert.Equal (Colors.Base.HotNormal, deserializedColors.HotNormal);
			Assert.Equal (Colors.Base.HotFocus, deserializedColors.HotFocus);
			Assert.Equal (Colors.Base.Disabled, deserializedColors.Disabled);
		}

		[Fact, AutoInitShutdown]
		public void TestVisualStyleManagerToJson ()
		{
			var visualStyle = new VisualStyle ();
			visualStyle.ColorSchemes = Colors.ColorSchemes;
			var json = VisualStyleManager.ToJson (visualStyle);

			var readVisualStyle = VisualStyleManager.LoadFromJson (json);

			Assert.Equal (Colors.Base.Normal, readVisualStyle.ColorSchemes ["Base"].Normal);
		}

		[Fact, AutoInitShutdown]
		public void TestVisualStyleManagerInitDriver ()
		{
			var visualStyle = new VisualStyle ();
			visualStyle.ColorSchemes = Colors.ColorSchemes;

			// Change Base
			var json = VisualStyleManager.ToJson (visualStyle);

			var readVisualStyle = VisualStyleManager.LoadFromJson (json);
			Assert.Equal (Colors.Base, readVisualStyle.ColorSchemes ["Base"]);
			Assert.Equal (Colors.TopLevel, readVisualStyle.ColorSchemes ["TopLevel"]);
			Assert.Equal (Colors.Error, readVisualStyle.ColorSchemes ["Error"]);
			Assert.Equal (Colors.Dialog, readVisualStyle.ColorSchemes ["Dialog"]);
			Assert.Equal (Colors.Menu, readVisualStyle.ColorSchemes ["Menu"]);

			Colors.Base = readVisualStyle.ColorSchemes["Base"];
			Colors.TopLevel = readVisualStyle.ColorSchemes ["TopLevel"];
			Colors.Error = readVisualStyle.ColorSchemes ["Error"];
			Colors.Dialog = readVisualStyle.ColorSchemes ["Dialog"];
			Colors.Menu = readVisualStyle.ColorSchemes ["Menu"];

			Assert.Equal (readVisualStyle.ColorSchemes ["Base"], Colors.Base);
			Assert.Equal (readVisualStyle.ColorSchemes ["TopLevel"], Colors.TopLevel);
			Assert.Equal (readVisualStyle.ColorSchemes ["Error"], Colors.Error);
			Assert.Equal (readVisualStyle.ColorSchemes ["Dialog"], Colors.Dialog);
			Assert.Equal (readVisualStyle.ColorSchemes ["Menu"], Colors.Menu);
		}

		[Fact, AutoInitShutdown]
		public void TestVisualStyleManagerLoadsFromJson ()
		{
			// Arrange
			string json = @"
			{
			""ColorSchemes"": {
				""Base"": {
					""NORMAL"": {
						""FOREGROUND"": ""WHITE"",
						""background"": ""blue""
	    					    },
					""focus"": {
						""Foreground"": ""bLAck"",
						""Background"": ""Gray""
					    },
					""hotNormal"": {
						""foreground"": ""BrightCyan"",
						""background"": ""Blue""
					    },
					""HotFocus"": {
						""foreground"": ""BrightBlue"",
						""background"": ""Gray""
					    },
					""disabled"": {
						""foreground"": ""DarkGray"",
						""background"": ""Blue""
					    }
				},
				""TopLevel"": {
					""NORMAL"": {
						""FOREGROUND"": ""WHITE"",
						""background"": ""blue""
	    					    },
					""focus"": {
						""Foreground"": ""bLAck"",
						""Background"": ""Gray""
					    },
					""hotNormal"": {
						""foreground"": ""BrightCyan"",
						""background"": ""Blue""
					    },
					""HotFocus"": {
						""foreground"": ""BrightBlue"",
						""background"": ""Gray""
					    },
					""disabled"": {
						""foreground"": ""DarkGray"",
						""background"": ""Blue""
					    }
				}
				}
			}";

			var visualStyle = VisualStyleManager.LoadFromJson (json);

			Assert.Equal (Color.White, visualStyle.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, visualStyle.ColorSchemes ["Base"].Normal.Background);

			Assert.Equal (Color.Black, visualStyle.ColorSchemes ["Base"].Focus.Foreground);
			Assert.Equal (Color.Gray, visualStyle.ColorSchemes ["Base"].Focus.Background);

			Assert.Equal (Color.BrightCyan, visualStyle.ColorSchemes ["Base"].HotNormal.Foreground);
			Assert.Equal (Color.Blue, visualStyle.ColorSchemes ["Base"].HotNormal.Background);

			Assert.Equal (Color.BrightBlue, visualStyle.ColorSchemes ["Base"].HotFocus.Foreground);
			Assert.Equal (Color.Gray, visualStyle.ColorSchemes ["Base"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, visualStyle.ColorSchemes ["Base"].Disabled.Foreground);
			Assert.Equal (Color.Blue, visualStyle.ColorSchemes ["Base"].Disabled.Background);

			Assert.Equal (Color.White, visualStyle.ColorSchemes ["TopLevel"].Normal.Foreground);
			Assert.Equal (Color.Blue, visualStyle.ColorSchemes ["TopLevel"].Normal.Background);

			Assert.Equal (Color.Black, visualStyle.ColorSchemes ["TopLevel"].Focus.Foreground);
			Assert.Equal (Color.Gray, visualStyle.ColorSchemes ["TopLevel"].Focus.Background);

			Assert.Equal (Color.BrightCyan, visualStyle.ColorSchemes ["TopLevel"].HotNormal.Foreground);
			Assert.Equal (Color.Blue, visualStyle.ColorSchemes ["TopLevel"].HotNormal.Background);

			Assert.Equal (Color.BrightBlue, visualStyle.ColorSchemes ["TopLevel"].HotFocus.Foreground);
			Assert.Equal (Color.Gray, visualStyle.ColorSchemes ["TopLevel"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, visualStyle.ColorSchemes ["TopLevel"].Disabled.Foreground);
			Assert.Equal (Color.Blue, visualStyle.ColorSchemes ["TopLevel"].Disabled.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestVisualStyleManagerSaveDefaults ()
		{
			VisualStyleManager.SaveDefaultStylesToFile ("visualstyles.json");

			// Verify the saved file matches

		}

		[Fact, AutoInitShutdown]
		public void TestVisualStyleManagerLoadDefaults ()
		{
			Assert.Null (VisualStyleManager.Defaults);
			VisualStyleManager.LoadDefaults ();
			Assert.NotNull (VisualStyleManager.Defaults);

			// Apply default styles
			VisualStyleManager.ApplyStyles (VisualStyleManager.Defaults);

			Assert.Equal (Color.White, VisualStyleManager.Defaults.ColorSchemes ["Base"].Normal.Foreground);
			Assert.Equal (Color.Blue, VisualStyleManager.Defaults.ColorSchemes ["Base"].Normal.Background);

			Assert.Equal (Color.Black, VisualStyleManager.Defaults.ColorSchemes ["Base"].Focus.Foreground);
			Assert.Equal (Color.Gray, VisualStyleManager.Defaults.ColorSchemes ["Base"].Focus.Background);

			Assert.Equal (Color.BrightCyan, VisualStyleManager.Defaults.ColorSchemes ["Base"].HotNormal.Foreground);
			Assert.Equal (Color.Blue, VisualStyleManager.Defaults.ColorSchemes ["Base"].HotNormal.Background);

			Assert.Equal (Color.BrightBlue, VisualStyleManager.Defaults.ColorSchemes ["Base"].HotFocus.Foreground);
			Assert.Equal (Color.Gray, VisualStyleManager.Defaults.ColorSchemes ["Base"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, VisualStyleManager.Defaults.ColorSchemes ["Base"].Disabled.Foreground);
			Assert.Equal (Color.Blue, VisualStyleManager.Defaults.ColorSchemes ["Base"].Disabled.Background);

			Assert.Equal (Color.BrightGreen, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].Normal.Foreground);
			Assert.Equal (Color.Black, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].Normal.Background);

			Assert.Equal (Color.White, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].Focus.Foreground);
			Assert.Equal (Color.Cyan, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].Focus.Background);

			Assert.Equal (Color.Brown, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].HotNormal.Foreground);
			Assert.Equal (Color.Black, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].HotNormal.Background);

			Assert.Equal (Color.Blue, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].HotFocus.Foreground);
			Assert.Equal (Color.Cyan, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].HotFocus.Background);

			Assert.Equal (Color.DarkGray, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].Disabled.Foreground);
			Assert.Equal (Color.Black, VisualStyleManager.Defaults.ColorSchemes ["TopLevel"].Disabled.Background);
		}
	}

	public class ColorJsonConverterTests {

		[Theory]
		[InlineData ("Black", Color.Black)]
		[InlineData ("Blue", Color.Blue)]
		[InlineData ("BrightBlue", Color.BrightBlue)]
		[InlineData ("BrightCyan", Color.BrightCyan)]
		[InlineData ("BrightGreen", Color.BrightGreen)]
		[InlineData ("BrightMagenta", Color.BrightMagenta)]
		[InlineData ("BrightRed", Color.BrightRed)]
		[InlineData ("BrightYellow", Color.BrightYellow)]
		[InlineData ("Brown", Color.Brown)]
		[InlineData ("Cyan", Color.Cyan)]
		[InlineData ("DarkGray", Color.DarkGray)]
		[InlineData ("Gray", Color.Gray)]
		[InlineData ("Green", Color.Green)]
		[InlineData ("Magenta", Color.Magenta)]
		[InlineData ("Red", Color.Red)]
		[InlineData ("White", Color.White)]
		public void TestColorDeserializationFromHumanReadableColorNames (string colorName, Color expectedColor)
		{
			// Arrange
			string json = $"\"{colorName}\"";

			// Act
			Color actualColor = JsonSerializer.Deserialize<Color> (json, VisualStyleMangerTests._jsonOptions);

			// Assert
			Assert.Equal (expectedColor, actualColor);
		}


		[Theory]
		[InlineData (Color.Black, "Black")]
		[InlineData (Color.Blue, "Blue")]
		[InlineData (Color.Green, "Green")]
		[InlineData (Color.Cyan, "Cyan")]
		[InlineData (Color.Gray, "Gray")]
		[InlineData (Color.Red, "Red")]
		[InlineData (Color.Magenta, "Magenta")]
		[InlineData (Color.Brown, "Brown")]
		[InlineData (Color.DarkGray, "DarkGray")]
		[InlineData (Color.BrightBlue, "BrightBlue")]
		[InlineData (Color.BrightGreen, "BrightGreen")]
		[InlineData (Color.BrightCyan, "BrightCyan")]
		[InlineData (Color.BrightRed, "BrightRed")]
		[InlineData (Color.BrightMagenta, "BrightMagenta")]
		[InlineData (Color.BrightYellow, "BrightYellow")]
		[InlineData (Color.White, "White")]
		public void SerializesEnumValuesAsStrings (Color color, string expectedJson)
		{
			var converter = new ColorJsonConverter ();
			var options = new JsonSerializerOptions { Converters = { converter } };

			var serialized = JsonSerializer.Serialize (color, options);

			Assert.Equal ($"\"{expectedJson}\"", serialized);
		}

		[Fact]
		public void TestSerializeColor_Black ()
		{
			// Arrange
			var color = Color.Black;
			var expectedJson = "\"Black\"";

			// Act
			var json = JsonSerializer.Serialize (color, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestSerializeColor_BrightRed ()
		{
			// Arrange
			var color = Color.BrightRed;
			var expectedJson = "\"BrightRed\"";

			// Act
			var json = JsonSerializer.Serialize (color, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedJson, json);
		}

		[Fact]
		public void TestDeserializeColor_Black ()
		{
			// Arrange
			var json = "\"Black\"";
			var expectedColor = Color.Black;

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}

		[Fact]
		public void TestDeserializeColor_BrightRed ()
		{
			// Arrange
			var json = "\"BrightRed\"";
			var expectedColor = Color.BrightRed;

			// Act
			var color = JsonSerializer.Deserialize<Color> (json, new JsonSerializerOptions {
				Converters = { new ColorJsonConverter () }
			});

			// Assert
			Assert.Equal (expectedColor, color);
		}
	}

	public class AttributeJsonConverterTests {
		[Fact, AutoInitShutdown]
		public void TestDeserialize ()
		{
			// Test deserializing from human-readable color names
			var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
			var attribute = JsonSerializer.Deserialize<Attribute> (json, VisualStyleMangerTests._jsonOptions);
			Assert.Equal (Color.Blue, attribute.Foreground);
			Assert.Equal (Color.Green, attribute.Background);

			// Test deserializing from RGB values
			json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
			attribute = JsonSerializer.Deserialize<Attribute> (json, VisualStyleMangerTests._jsonOptions);
			Assert.Equal (Color.BrightRed, attribute.Foreground);
			Assert.Equal (Color.BrightGreen, attribute.Background);
		}

		[Fact, AutoInitShutdown]
		public void TestSerialize ()
		{
			// Test serializing to human-readable color names
			var attribute = new Attribute (Color.Blue, Color.Green);
			var json = JsonSerializer.Serialize<Attribute> (attribute, VisualStyleMangerTests._jsonOptions);
			Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\"}", json);
		}
	}

	public class ColorSchemeJsonConverterTests
	{
		//string json = @"
		//	{
		//	""ColorSchemes"": {
		//		""Base"": {
		//			""normal"": {
		//				""foreground"": ""White"",
		//				""background"": ""Blue""
		//   		            },
		//			""focus"": {
		//				""foreground"": ""Black"",
		//				""background"": ""Gray""
		//			    },
		//			""hotNormal"": {
		//				""foreground"": ""BrightCyan"",
		//				""background"": ""Blue""
		//			    },
		//			""hotFocus"": {
		//				""foreground"": ""BrightBlue"",
		//				""background"": ""Gray""
		//			    },
		//			""disabled"": {
		//				""foreground"": ""DarkGray"",
		//				""background"": ""Blue""
		//			    }
		//		}
		//		}
		//	}";
		[Fact, AutoInitShutdown]
		public void TestColorSchemeSerialization ()
		{
			// Arrange
			var expectedColorScheme = new ColorScheme {
				Normal = Attribute.Make (Color.White, Color.Blue),
				Focus = Attribute.Make (Color.Black, Color.Gray),
				HotNormal = Attribute.Make (Color.BrightCyan, Color.Blue),
				HotFocus = Attribute.Make (Color.BrightBlue, Color.Gray),
				Disabled = Attribute.Make (Color.DarkGray, Color.Blue)
			};
			var serializedColorScheme = JsonSerializer.Serialize<ColorScheme> (expectedColorScheme, VisualStyleMangerTests._jsonOptions);

			// Act
			var actualColorScheme = JsonSerializer.Deserialize<ColorScheme> (serializedColorScheme, VisualStyleMangerTests._jsonOptions);

			// Assert
			Assert.Equal (expectedColorScheme, actualColorScheme);
		}
	}
}

