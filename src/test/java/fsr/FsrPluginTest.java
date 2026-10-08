package fsr;

import net.runelite.client.RuneLite;
import net.runelite.client.externalplugins.ExternalPluginManager;

public final class FsrPluginTest
{
	private FsrPluginTest()
	{
	}

	public static void main(String[] args) throws Exception
	{
		ExternalPluginManager.loadBuiltin(FsrPlugin.class);
		RuneLite.main(args);
	}
}
