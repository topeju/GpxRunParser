using RazorEngine.Templating;

namespace GpxRunParser.Templates
{
	public class ModelTemplate<T> : TemplateBase<T>
	{
		public new T Model { get; set; }
	}
}
