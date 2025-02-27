using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text;

using Xunit;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;

namespace NReco.Linq.Tests {

	class ThisCompare : ValueComparer
    {
		protected override object Penetrate(object obj)
		{
			if (obj is JToken token)
            {
				var ob = token.ToObject<object>();
				return ob;
            }
			return obj;
		}
	}

	public class LambdaParserTests {

		Dictionary<string,object> getContext() {
			var varContext = new Dictionary<string, object>();
			varContext["pi"] = 3.14M;
			varContext["one"] = 1M;
			varContext["two"] = 2M;
			varContext["test"] = "test";
			varContext["now"] = DateTime.Now;
			varContext["testObj"] = new TestClass();
			varContext["getTestObj"] = (Func<TestClass>)(() => new TestClass());
			varContext["toString"] = (Func<object,string>)((o) => o.ToString());
			varContext["arr1"] = new double[] { 1.5, 2.5 };
			varContext["NOT"] = (Func<bool, bool>)((t) => !t);
			varContext["Yes"] = true;
			varContext["nullVar"] = null;
			varContext["name_with_underscore"] = "a_b";
			varContext["_name_with_underscore"] = "_a_b";
			varContext["day1"] = new DateTime().AddDays(1);
			varContext["day2"] = new DateTime().AddDays(2);
			varContext["oneDay"] = new TimeSpan(1,0,0,0);
			varContext["twoDays"] = new TimeSpan(2,0,0,0);
			return varContext;
		}

		[Fact]
		public void EvalJ()
		{
			string exre = "a > 2";
			Dictionary<string, object> dic = new Dictionary<string, object>
			{
				{ "a", new JValue(3)}
			};
			//JValue jValue = new JValue(3);
			var lambdaParser = new LambdaParser(new ThisCompare());
			
			try
			{
				var ret = lambdaParser.Eval(exre, dic);
				Console.WriteLine(ret);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.StackTrace);
			}
		}

	    [Fact]
		public void Eval() {
			var lambdaParser = new LambdaParser();

			var varContext = getContext();

			Assert.Equal("st", lambdaParser.Eval("test.Substring(2)", varContext ) );

			Assert.Equal(true, lambdaParser.Eval("NOT(NOT(1==1))", varContext));

			Assert.Equal(3M, lambdaParser.Eval("1+2", varContext) );
			Assert.Equal(6M, lambdaParser.Eval("1+2+3", varContext));
			Assert.Equal("b{0}_", lambdaParser.Eval("\"b{0}_\"", varContext));

			Assert.Equal(3M, lambdaParser.Eval("(1+(3-1)*4)/3", varContext));
			
			Assert.Equal(1M, lambdaParser.Eval("one*5*one-(-1+5*5%10)", varContext));

			Assert.Equal("ab", lambdaParser.Eval("\"a\"+\"b\"", varContext));

			Assert.Equal(4.14M, lambdaParser.Eval("pi + 1", varContext) );

			Assert.Equal(5.14M, lambdaParser.Eval("2 +pi", varContext) );

			Assert.Equal(2.14M, lambdaParser.Eval("pi + -one", varContext) );

			Assert.Equal("test1", lambdaParser.Eval("test + \"1\"", varContext) );

			Assert.Equal("a_b_a_b", lambdaParser.Eval(" name_with_underscore + _name_with_underscore ", varContext));

			Assert.Equal(1M, lambdaParser.Eval("true or false ? 1 : 0", varContext) );

			Assert.Equal(true, lambdaParser.Eval("5<=3 ? false : true", varContext));

			Assert.Equal(5M, lambdaParser.Eval("pi>one && 0<one ? (1+8)/3+1*two : 0", varContext));

			Assert.Equal(4M, lambdaParser.Eval("pi>0 ? one+two+one : 0", varContext));

			Assert.Equal(DateTime.Now.Year, lambdaParser.Eval("now.Year", varContext) );

			Assert.Equal(true, lambdaParser.Eval(" (1+testObj.IntProp)==2 ? testObj.FldTrue : false ", varContext));

			Assert.Equal("ab2_3", lambdaParser.Eval(" \"a\"+testObj.Format(\"b{0}_{1}\", 2, \"3\".ToString() ).ToString() ", varContext));

			Assert.Equal(true, lambdaParser.Eval(" testObj.Hash[\"a\"] == \"1\"", varContext));
			
			Assert.Equal(true, lambdaParser.Eval(" (testObj.Hash[\"a\"]-1)==testObj.Hash[\"b\"].Length ", varContext));

			Assert.Equal(4.0M, lambdaParser.Eval(" arr1[0]+arr1[1] ", varContext));

			Assert.Equal(2M, lambdaParser.Eval(" (new[]{1,2})[1] ", varContext));

			Assert.Equal(true, lambdaParser.Eval(" new[]{ one } == new[] { 1 } ", varContext));

			Assert.Equal(3, lambdaParser.Eval(" new dictionary{ {\"a\", 1}, {\"b\", 2}, {\"c\", 3} }.Count ", varContext));

			Assert.Equal(2M, lambdaParser.Eval(" new dictionary{ {\"a\", 1}, {\"b\", 2}, {\"c\", 3} }[\"b\"] ", varContext));

			var arr = ((Array)lambdaParser.Eval(" new []{ new dictionary{{\"test\",2}}, new[] { one } }", varContext) );
			Assert.Equal(2M, ((IDictionary)arr.GetValue(0) )["test"] );
			Assert.Equal(1M, ((Array)arr.GetValue(1) ).GetValue(0) );

			Assert.Equal("str", lambdaParser.Eval(" testObj.GetDelegNoParam()() ", varContext));
			Assert.Equal("zzz", lambdaParser.Eval(" testObj.GetDelegOneParam()(\"zzz\") ", varContext));

			Assert.Equal(false, lambdaParser.Eval("(testObj.FldTrue and false) || (testObj.FldTrue && false)", varContext ) );
			Assert.Equal(true, lambdaParser.Eval("false or testObj.FldTrue", varContext ) );
			Assert.Equal("True", lambdaParser.Eval("testObj.BoolParam(true)", varContext ) );
			Assert.Equal("True", lambdaParser.Eval("getTestObj().BoolParam(true)", varContext));
			Assert.Equal("NReco.Linq.Tests.LambdaParserTests+TestClass", lambdaParser.Eval("toString(testObj)", varContext));

			Assert.True( (bool) lambdaParser.Eval("true && NOT( false )", varContext ) );
			Assert.True( (bool) lambdaParser.Eval("true && !( false )", varContext ) );
			Assert.False( (bool) lambdaParser.Eval("!Yes", varContext ) );

			Assert.True((bool)lambdaParser.Eval("5>two && (5>7 || test.Contains(\"t\") )", varContext));
			Assert.True((bool)lambdaParser.Eval("null!=test && test!=null && test.Contains(\"t\") && true == Yes && false==!Yes && false!=Yes", varContext));

			Assert.Equal(new DateTime().AddDays(2), lambdaParser.Eval("day1 + oneDay", varContext));
			Assert.Equal(new DateTime().AddDays(2), lambdaParser.Eval("oneDay + day1", varContext));
			Assert.Equal(new DateTime().AddDays(1), lambdaParser.Eval("day2 - oneDay", varContext));
			Assert.Equal(new DateTime().AddDays(1), lambdaParser.Eval("day2 + -oneDay", varContext));
			Assert.Equal(new DateTime().AddDays(1), lambdaParser.Eval("-oneDay + day2", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0), lambdaParser.Eval("day2 - day1", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0).Negate(), lambdaParser.Eval("day1 - day2", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0), lambdaParser.Eval("day2 - day1", varContext));
			Assert.Equal(new TimeSpan(2,0,0,0), lambdaParser.Eval("oneDay + oneDay", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0), lambdaParser.Eval("twoDays - oneDay", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0), lambdaParser.Eval("twoDays + -oneDay", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0).Negate(), lambdaParser.Eval("oneDay - twoDays", varContext));
			Assert.Equal(new TimeSpan(1,0,0,0).Negate(), lambdaParser.Eval("-twoDays + oneDay", varContext));
		}

		[Fact]
		public void SingleEqualSign() {
			var varContext = getContext();
			var lambdaParser = new LambdaParser();
			lambdaParser.AllowSingleEqualSign = true;
			//Assert.True((bool)lambdaParser.Eval("null = nullVar", varContext));
			//Assert.True((bool)lambdaParser.Eval("5 = (5+1-1)", varContext));

			string str = "\"4.00\" == 4.0";
			//var result = lambdaParser.Eval(str, varContext);

			//str = "4.0 == \"4.00\"";
			//result = lambdaParser.Eval(str, varContext);

			//str = "\"4.0\" == \"4.00\"";
			//result = lambdaParser.Eval(str, varContext);

			varContext["obj"] = true;
			var result = lambdaParser.Eval("\"===\" + obj", varContext);


			Assert.True((bool)result);
		}

		[Fact]
		public void Equals()
        {
			var varContext = getContext();
			var lambdaParser = new LambdaParser();
			lambdaParser.AllowSingleEqualSign = true;
			varContext["a"] = new string[] { "1", "2", "3"};
			varContext["b"] = 2;

			varContext["e"] = 3;
			varContext["f"] = 2;
			varContext["g"] = new object();
			var r = (bool)lambdaParser.Eval("\"[]\" == a", varContext);
			//var r = (int)lambdaParser.Eval("(a++)*(a++)", varContext);
			Assert.True(r);
		}

		[Fact]
		public void Subduction()
        {
			var varContext = getContext();
			var lambdaParser = new LambdaParser();
			lambdaParser.AllowSingleEqualSign = true;
			varContext["a"] = " 3.2 ";
			varContext["b"] = "2";
			var r = (Decimal)lambdaParser.Eval("a - b", varContext);
			Assert.True((r == 1));
		}

		

		[Fact]
		public void IndexOf()
        {
			var varContext = getContext();
			string exp = "false == \"1\"";
			var lambdaParser = new LambdaParser();
			lambdaParser.AllowSingleEqualSign = true;
			varContext["a"] = "A";
			var r = lambdaParser.Eval(exp, varContext);
			Console.WriteLine(r);
		}

		[Fact]
		public void NullComparison() {
			var varContext = getContext();
			varContext["a"] = 1400;
			var lambdaParser = new LambdaParser();

			//Assert.True((bool)lambdaParser.Eval("null == nullVar", varContext));
			//Assert.True((bool)lambdaParser.Eval("5>nullVar", varContext));
			//Assert.True((bool)lambdaParser.Eval("testObj!=null", varContext));
			//Assert.Equal(0, LambdaParser.GetExpressionParameters(lambdaParser.Parse("20 == null")).Length);

			//lambdaParser = new LambdaParser(new ValueComparer() { NullComparison = ValueComparer.NullComparisonMode.Sql });
			//Assert.False((bool)lambdaParser.Eval("null == nullVar", varContext));
			//Assert.False((bool)lambdaParser.Eval("nullVar<5", varContext));
			//Assert.False((bool)lambdaParser.Eval("nullVar>5", varContext));

			var k = lambdaParser.Eval("\"1400.00\" == a", varContext);

			Console.WriteLine(k);
		}

		[Fact]
		public void EvalCachePerf() {
			var lambdaParser = new LambdaParser();

			var varContext = new Dictionary<string, object>();
			varContext["a"] = 55;
			varContext["b"] = 2;

			var sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < 10000; i++) {

				Assert.Equal(105M, lambdaParser.Eval("(a*2 + 100)/b", varContext));
			}
			sw.Stop();
			Console.WriteLine("10000 iterations: {0}", sw.Elapsed);
		}
		[Fact]
		public void EvalAttribute()
        {
			var lambdaParser = new LambdaParser();

			var varContext = new Dictionary<string, object>();
			varContext["a"] = new Dictionary<string, object>
			{
				{ "2", "Jack" }
			};
			varContext["b"] = 2;
			var value = lambdaParser.Eval("a.a2 + 3", varContext);
			Console.WriteLine(value);
		}

	    [Fact]
		public void ExtMethod()
        {
			var lambdaParser = new LambdaParser();

			var varContext = new Dictionary<string, object>();
			varContext["a"] = new Dictionary<string, object>
			{
				{ "2", "Jack" }
			};
			varContext["b"] = false;
			varContext["c"] = false;
			var value = lambdaParser.Eval("b 假", varContext);
			Console.WriteLine(value);
		}

		[Fact]
		public void ExtSumMethod()
		{
			var lambdaParser = new LambdaParser();

            var varContext = new Dictionary<string, object>
            {
                ["b"] = "2",
                ["c"] = 3
            };
            //var value = lambdaParser.Eval("b * c", varContext);
            var value = lambdaParser.Eval("\"1\" + 1", varContext);



			Console.WriteLine(value);
		}

		internal T DeserializeObject<T>(string value)
		{
			var serializerSettings = new JsonSerializerSettings
			{
				// 设置为驼峰命名
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};
			return JsonConvert.DeserializeObject<T>(value, serializerSettings);
		}

		[Fact]
		public void ExtSumMethod2()
		{
			var lambdaParser = new LambdaParser();
			string str = "{\"result\":{\"name\":\"Jack\", \"age\":15}}";
			var obj = DeserializeObject<Dictionary<string, object>>(str);
			var varContext = new Dictionary<string, object>
			{
				{ "a" , null }
			};


			var value = lambdaParser.Eval("\"1\" == 1", varContext);
			Console.WriteLine(value);

			//str = "43.0";
			//Regex regex = new Regex($"^[+-]?(\\d+.)?\\d+$");
			//bool isD =  regex.IsMatch(str);
			//Console.WriteLine(isD);
		}

		[Fact]
		public void TestCompare()
        {
			var context = new Dictionary<string, object>
			{
				{ "当前页", "2" },
				{ "总页数", "16a" },
			};
			string str = "当前页 < 总页数";
			var lambdaParser = new LambdaParser();
			var value = lambdaParser.Eval(str, context);
			Console.WriteLine(value);
		}


		[Fact]
		public void RegString()
        {
			string s = "\"msg:\\\"([^\\\"]*)\\\"\"";
			var varContext = new Dictionary<string, object>
			{
				{ "a" , null }
			};
			var lambdaParser = new LambdaParser();
			var rs = lambdaParser.Eval(s, varContext);
			Console.WriteLine(rs);
		}

		public class TestClass {

			public int IntProp { get { return 1; } }

			public string StrProp { get { return "str"; } }

			public bool FldTrue { get { return true; } }

			public IDictionary Hash {
				get {
					return new Hashtable() {
						{"a", 1},
						{"b", ""}
					};
				}
			}

			public string Format(string s, object arg1, int arg2) {
				return String.Format(s, arg1, arg2);
			}

			public string BoolParam(bool flag) {
				return flag.ToString();
			}

			public Func<string, string> GetDelegOneParam() {
				return (s) => {
					return s;
				};
			}

			public Func<string> GetDelegNoParam() {
				return () => {
					return StrProp;
				};
			}


		}

	}
}
