package fsr;

import net.runelite.client.RuneLite;
import net.runelite.client.externalplugins.ExternalPluginManager;
import runelitehitchprofiler.RuneLiteHitchProfilerPlugin;

public final class CombinedRuneLiteLauncher
{
	private CombinedRuneLiteLauncher()
	{
	}

	public static void main(String[] args) throws Exception
	{
		ExternalPluginManager.loadBuiltin(FsrPlugin.class, RuneLiteHitchProfilerPlugin.class);
		RuneLite.main(args);
	}
}
