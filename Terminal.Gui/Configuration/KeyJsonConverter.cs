﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// Support for <see cref="Key"/> in JSON in the form of "Ctrl-X" or "Alt-Shift-F1".
/// </summary>
public class KeyJsonConverter : JsonConverter<Key> {
	public override Key Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Key.TryParse (reader.GetString (), out var key) ? key : Key.Empty;

	public override void Write (Utf8JsonWriter writer, Key value, JsonSerializerOptions options) => writer.WriteStringValue (value.ToString ());
}