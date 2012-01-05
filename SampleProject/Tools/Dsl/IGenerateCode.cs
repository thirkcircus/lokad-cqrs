using System.CodeDom.Compiler;

namespace Dsl
{
	public interface IGenerateCode
	{
		void Generate(Context context, IndentedTextWriter writer);
	}
}