namespace Pluton.Core
{
	using System;
	using System.Collections.Generic;

	public interface ISingleton
	{
		bool CheckDependencies();
		void Initialize();
	}

	public interface ISingleton<T>
	{
		List<Func<bool>> Dependencies { get; }
	}
}

