using AllConstantsGenerator;

namespace AllConstantsGenerator
{
	namespace Y
	{
		[AddAllStringConstants]
		public partial class Program
		{
			public const string COLUMN1 = "Program1";
			public const string COLUMN2 = "Program2";

			private static void Main(string[] args)
			{
				Console.WriteLine("=={0}==", typeof(WithoutNamespace<int,int>));
				foreach(var c in WithoutNamespace<int, int>.ALL)
					Console.WriteLine(c);
				Console.WriteLine("=={0}==", typeof(Program));
				foreach(var c in Program.ALL)
					Console.WriteLine(c);
				Console.WriteLine("=={0}==", typeof(InNamespace<int, string>));
				foreach(var c in InNamespace<int, string>.ALL)
					Console.WriteLine(c);
				Console.WriteLine("=={0}==", typeof(SomeParentStruct<object>.NestedEvenMore<int, string>));
				foreach(var c in SomeParentStruct<object>.NestedEvenMore<int, string>.ALL)
					Console.WriteLine(c);
				//Compile time error 'ShouldNotGenerate' does not contain a definition for 'ALL'
				//Because there is no AddAllStringConstantsAttribute
				//foreach(var c in ShouldnotGenerate.ALL)
				//	Console.WriteLine(c);
			}

			public partial struct SomeParentStruct
			{
				[AddAllStringConstants]
				public partial class NestedEvenMore
				{
					public const string COLUMN1 = "SomeParentStruct.NestedEvenMore1";
					public const string COLUMN2 = "SomeParentStruct.NestedEvenMore2";
				}
			}

			public partial struct SomeParentStruct<D>
			{
				[AddAllStringConstants]
				public partial class NestedEvenMore<T, U> : List<T> where T : struct
				{
					public const string COLUMN1 = "SomeParentStruct.NestedEvenMore1";
					public const string COLUMN2 = "SomeParentStruct.NestedEvenMore2";
				}
			}
			[AddAllStringConstants]
			public partial class SameNameDifferentOuter
			{
				public const string COLUMN1 = "Test.Y.Program.SameNameDifferentOuter1";
			}
		}
		[AddAllStringConstants]
		public partial class SameNameDifferentOuter
		{
			public const string COLUMN1 = "Test.Y.SameNameDifferentOuter1";
		}
	}

	[AddAllStringConstants]
	public partial class SameNameDifferentOuter
	{
		public const string COLUMN1 = "Test.SameNameDifferentOuter1";
	}

	[AddAllStringConstants]
	public partial class InNamespace<T, U> : List<T> where T : struct
	{
		public const string COLUMN1 = "InNamespace1";
		public const string COLUMN2 = "InNamespace2";
	}

	public partial class ShouldNotGenerate
	{
		public const string COLUMN1 = "ShouldNotGenerate1";
		public const String COLUMN2 = "ShouldNotGenerate2";
	}
}

[AddAllStringConstants]
public partial class WithoutNamespace<T, U> : List<T> where T : struct
{
	//string works
	public const string COLUMN1 = "WithoutNamespace1";

	//String works
	public const String COLUMN2 = "WithoutNamespace2";

	//System.String works
	public const System.String COLUMN3 = "WithoutNamespace3";

	//multiple works
	public const string COLUMN4 = "WithoutNamespace3", COLUMN5 = "WithoutNamespace4";

	//not included as not string
	public const int COLUMN6 = 6;
}