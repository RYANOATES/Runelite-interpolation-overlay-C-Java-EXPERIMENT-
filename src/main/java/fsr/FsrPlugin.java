package fsr;

import com.google.inject.Provides;
import java.io.File;
import java.io.IOException;
import java.util.concurrent.TimeUnit;
import javax.inject.Inject;
import lombok.extern.slf4j.Slf4j;
import net.runelite.client.config.ConfigManager;
import net.runelite.client.input.KeyManager;
import net.runelite.client.plugins.Plugin;
import net.runelite.client.plugins.PluginDescriptor;
import net.runelite.client.util.HotkeyListener;

@Slf4j
@PluginDescriptor(
	name = "Frame Interpolation Overlay (Experimental)",
	description = "Experimental desktop frame interpolation overlay for RuneLite",
	tags = {"gpu", "graphics", "frame generation", "experimental"}
)
public class FsrPlugin extends Plugin
{
	@Provides
	FsrConfig provideConfig(ConfigManager configManager)
	{
		return configManager.getConfig(FsrConfig.class);
	}

	@Inject
	private FsrConfig config;

	@Inject
	private KeyManager keyManager;

	private Process companion;
	private HotkeyListener toggleListener;

	@Override
	protected void startUp()
	{
		toggleListener = new HotkeyListener(config::toggleHotkey)
		{
			@Override
			public void hotkeyPressed()
			{
				if (companion != null && companion.isAlive()) stopCompanion();
				else startCompanion();
			}
		};
		keyManager.registerKeyListener(toggleListener);
		startCompanion();
	}

	@Override
	protected void shutDown()
	{
		keyManager.unregisterKeyListener(toggleListener);
		stopCompanion();
	}

	private void startCompanion()
	{
		if (companion != null && companion.isAlive()) return;
		String path = config.companionPath().trim();
		File executable = new File(path);
		if (path.isEmpty() || !executable.isFile())
		{
			log.warn("Frame overlay not started. Set the Overlay executable path to the published FrameInterpolationOverlay.exe.");
			return;
		}
		try
		{
			companion = new ProcessBuilder(executable.getAbsolutePath())
				.directory(executable.getParentFile())
				.start();
			log.info("Started frame interpolation overlay: {}", executable);
		}
		catch (IOException ex)
		{
			log.error("Unable to start frame interpolation overlay", ex);
		}
	}

	private void stopCompanion()
	{
		if (companion == null) return;
		if (companion.isAlive())
		{
			companion.destroy();
			try
			{
				if (!companion.waitFor(2, TimeUnit.SECONDS)) companion.destroyForcibly();
			}
			catch (InterruptedException ex)
			{
				Thread.currentThread().interrupt();
				companion.destroyForcibly();
			}
		}
		companion = null;
	}
}
