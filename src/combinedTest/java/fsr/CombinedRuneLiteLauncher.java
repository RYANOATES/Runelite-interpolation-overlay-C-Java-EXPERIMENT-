package fsr;

import net.runelite.client.RuneLite;
import net.runelite.client.externalplugins.ExternalPluginManager;
import sailingloadprofiler.SailingLoadProfilerPlugin;

public final class CombinedRuneLiteLauncher
{
	private CombinedRuneLiteLauncher()
	{
	}

	public static void main(String[] args) throws Exception
	{
		ExternalPluginManager.loadBuiltin(FsrPlugin.class, SailingLoadProfilerPlugin.class);
		RuneLite.main(args);
	}
}
