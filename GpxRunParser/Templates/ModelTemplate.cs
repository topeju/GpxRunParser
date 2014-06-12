using RazorEngine.Templating;

namespace GpxRunParser.Templates
{
	public class ModelTemplate<T> : TemplateBase<T>
	{
		public T Model { get; set; }
	}
}
