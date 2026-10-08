package fsr;

import net.runelite.client.config.Config;
import net.runelite.client.config.ConfigGroup;
import net.runelite.client.config.ConfigItem;
import net.runelite.client.config.Keybind;

@ConfigGroup("fsrrenderscale")
public interface FsrConfig extends Config
{
	@ConfigItem(
		keyName = "status",
		name = "Overlay status",
		description = "Shows the current frame interpolation overlay status."
	)
	default String status()
	{
		return "Experimental capture overlay; motion interpolation may ghost UI";
	}

	@ConfigItem(
		keyName = "companionPath",
		name = "Overlay executable",
		description = "Path to the published FrameInterpolationOverlay.exe companion."
	)
	default String companionPath()
	{
		return "";
	}

	@ConfigItem(
		keyName = "toggleHotkey",
		name = "Toggle overlay hotkey",
		description = "Starts or stops the frame interpolation overlay."
	)
	default Keybind toggleHotkey()
	{
		return Keybind.NOT_SET;
	}
}
