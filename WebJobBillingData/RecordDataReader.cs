//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage and Billing Insights
//
//Copyright(c) Microsoft Corporation
//
//All rights reserved.
//
//MIT License
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the ""Software""), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial 
//portions of the Software.
//
//THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------- END OF LICENSE ------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

public class RecordDataReader<T> : DbDataReader
{
	private int _recordCount = 0;
	private readonly Func<T, bool> _sink;
	private T _currentRecord;
	public readonly Dictionary<string, int> MaxLenghts;
	private readonly bool _trackMaxLenghts;
	private readonly IEnumerable<T> _recordSource;
	private readonly IEnumerator<T> _enumerator;
	private readonly PropertyInfo[] _propMap;

	public RecordDataReader(IEnumerable<T> recordSource, Func<T, bool> sink = null, bool trackMaxLenghts = false)
	{
		_recordSource = recordSource;
		_sink = sink;
		_trackMaxLenghts = trackMaxLenghts;
		_enumerator = _recordSource.GetEnumerator();

		_propMap = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

		if (trackMaxLenghts) {
			MaxLenghts = new Dictionary<string, int>(FieldCount);

			for (int i = 0; i < FieldCount; i++) {
				MaxLenghts.Add(GetName(i), 0);
			}
		}
	}

	public override int FieldCount {
		get { return _propMap.Length; }
	}

	public override int RecordsAffected {
		get { return _recordCount; }
	}

	public override bool HasRows {
		get { return true; }
	}

	protected override void Dispose(bool disposing)
	{
		_enumerator.Dispose();
	}

	public override string GetName(int i)
	{
		return _propMap[i].Name;
	}

	public override int GetOrdinal(string name)
	{
		var map = _propMap.Single(m => m.Name == name);
		return Array.IndexOf(_propMap, map);
	}

	public override object GetValue(int i)
	{
		PropertyInfo prop = _propMap[i];
		object value = prop.GetValue(_currentRecord);

		if (value is IDictionary) {
			string json = SerializeDictionary((IDictionary)value);
			return json;
		} else if (value is ICollection) {
			string json = JsonConvert.SerializeObject(value);
			return json;
		} else {
			return value;
		}
	}

	public override bool IsDBNull(int i)
	{
		object value = GetValue(i);
		return value == null;
	}

	public override bool NextResult()
	{
		return false; // single result only
	}

	public override bool Read()
	{
		do {
			if (!_enumerator.MoveNext()) return false;
			_currentRecord = _enumerator.Current;

			if (_trackMaxLenghts) {
				for (int i = 0; i < FieldCount; i++) {
					string fieldName = GetName(i);
					int fieldLength = MaxLenghts[fieldName];
					object val = GetValue(i);

					if (val != null) {
						string s = val.ToString();
						if (s.Length > fieldLength) MaxLenghts[fieldName] = s.Length;
					}
				}
			}
		} while (_sink != null && !_sink(_currentRecord));

		_recordCount++;

		return true;
	}

	public override Type GetFieldType(int i)
	{
		PropertyInfo prop = _propMap[i];

		if (prop.PropertyType is IDictionary) {
			// value serialized to JSON string
			return typeof(string);
		} else if (prop.PropertyType is ICollection) {
			// value serialized to JSON string
			return typeof(string);
		} else {
			return prop.PropertyType;
		}
	}

	#region DbDataReader - not implemented

	public override object this[string name] {
		get { throw new NotImplementedException(); }
	}

	public override object this[int i] {
		get { throw new NotImplementedException(); }
	}

	public override int Depth {
		get { throw new NotImplementedException(); }
	}

	public override bool IsClosed {
		get { throw new NotImplementedException(); }
	}

	public override bool GetBoolean(int i)
	{
		throw new NotImplementedException();
	}

	public override byte GetByte(int i)
	{
		throw new NotImplementedException();
	}

	public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
	{
		throw new NotImplementedException();
	}

	public override char GetChar(int i)
	{
		throw new NotImplementedException();
	}

	public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
	{
		throw new NotImplementedException();
	}

	public override string GetDataTypeName(int i)
	{
		throw new NotImplementedException();
	}

	public override DateTime GetDateTime(int i)
	{
		throw new NotImplementedException();
	}

	public override decimal GetDecimal(int i)
	{
		throw new NotImplementedException();
	}

	public override double GetDouble(int i)
	{
		throw new NotImplementedException();
	}

	public override float GetFloat(int i)
	{
		throw new NotImplementedException();
	}

	public override Guid GetGuid(int i)
	{
		throw new NotImplementedException();
	}

	public override short GetInt16(int i)
	{
		throw new NotImplementedException();
	}

	public override int GetInt32(int i)
	{
		throw new NotImplementedException();
	}

	public override long GetInt64(int i)
	{
		throw new NotImplementedException();
	}

	public override string GetString(int i)
	{
		throw new NotImplementedException();
	}

	public override int GetValues(object[] values)
	{
		throw new NotImplementedException();
	}

	public override IEnumerator GetEnumerator()
	{
		throw new NotImplementedException();
	}

	#endregion

	private static string SerializeDictionary(IDictionary dictionary)
	{
		var textBuilder = new StringBuilder();

		using (var textWriter = new StringWriter(textBuilder)) {
			JsonTextWriter writer = new JsonTextWriter(textWriter);
			writer.WriteStartArray();

			foreach (object key in dictionary.Keys) {
				object value = dictionary[key];
				writer.WriteStartObject();
				writer.WritePropertyName("Name");
				writer.WriteValue(key.ToString());
				writer.WritePropertyName("Value");
				writer.WriteValue(value.ToString());
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
		}

		return textBuilder.ToString();
	}
}
