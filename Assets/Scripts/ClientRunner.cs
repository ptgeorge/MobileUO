using System;
using System.IO;
using System.Linq;
using UnityEngine;
using ClassicUO;
using ClassicUO.Utility.Logging;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.UI.Gumps.Login;
using Newtonsoft.Json;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using SDL2;
using GameObject = UnityEngine.GameObject;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

public class ClientRunner : MonoBehaviour
{
	[SerializeField]
	public bool useGraphicsDrawTexture;
	[SerializeField]
	private bool forceEnterWorld;
	[SerializeField]
	private bool scaleGameToFitScreen;
	[SerializeField]
	private MobileJoystick movementJoystick;
	[SerializeField]
	private bool showMovementJoystickOnNonMobilePlatforms;
	[SerializeField]
	private float[] joystickDeadZoneValues;
	[SerializeField]
	private float[] joystickRunThresholdValues;
	[SerializeField]
	private GameObject modifierKeyButtonsParent;
	[SerializeField]
	private ModifierKeyButtonPresenter ctrlKeyButtonPresenter;
	[SerializeField]
	private ModifierKeyButtonPresenter altKeyButtonPresenter;
	[SerializeField]
	private ModifierKeyButtonPresenter shiftKeyButtonPresenter;
	[SerializeField]
	private UnityEngine.UI.Button escButton;

	private int lastScreenWidth;
	private int lastScreenHeight;
	
	public Action<string> OnError;
	public Action OnExiting;
	public Action<bool> SceneChanged;

	private void Awake()
	{
		UserPreferences.ScaleSize.ValueChanged += OnCustomScaleSizeChanged;
		UserPreferences.ForceUseXbr.ValueChanged += OnForceUseXbrChanged;
		UserPreferences.ShowCloseButtons.ValueChanged += OnShowCloseButtonsChanged;
		UserPreferences.UseMouseOnMobile.ValueChanged += OnUseMouseOnMobileChanged;
		UserPreferences.TargetFrameRate.ValueChanged += OnTargetFrameRateChanged;
		UserPreferences.TextureFiltering.ValueChanged += UpdateTextureFiltering;
		UserPreferences.JoystickDeadZone.ValueChanged += OnJoystickDeadZoneChanged;
		UserPreferences.JoystickRunThreshold.ValueChanged += OnJoystickRunThresholdChanged;
		UserPreferences.ContainerItemSelection.ValueChanged += OnContainerItemSelectionChanged;
		UserPreferences.ShowModifierKeyButtons.ValueChanged += OnShowModifierKeyButtonsChanged;
		UserPreferences.EnableAssistant.ValueChanged += OnEnableAssistantChanged;
		UserPreferences.EnlargeSmallButtons.ValueChanged += OnEnlargeSmallButtonsChanged;
		OnCustomScaleSizeChanged(UserPreferences.ScaleSize.CurrentValue);
		OnForceUseXbrChanged(UserPreferences.ForceUseXbr.CurrentValue);
		OnShowCloseButtonsChanged(UserPreferences.ShowCloseButtons.CurrentValue);
		OnUseMouseOnMobileChanged(UserPreferences.UseMouseOnMobile.CurrentValue);
		OnTargetFrameRateChanged(UserPreferences.TargetFrameRate.CurrentValue);
		UpdateTextureFiltering(UserPreferences.TextureFiltering.CurrentValue);
		OnJoystickDeadZoneChanged(UserPreferences.JoystickDeadZone.CurrentValue);
		OnJoystickRunThresholdChanged(UserPreferences.JoystickRunThreshold.CurrentValue);
		OnContainerItemSelectionChanged(UserPreferences.ContainerItemSelection.CurrentValue);
		OnShowModifierKeyButtonsChanged(UserPreferences.ShowModifierKeyButtons.CurrentValue);
		OnEnableAssistantChanged(UserPreferences.EnableAssistant.CurrentValue);
		OnEnlargeSmallButtonsChanged(UserPreferences.EnlargeSmallButtons.CurrentValue);
		
		escButton.onClick.AddListener(OnEscButtonClicked);
	}

	private void OnDisable()
	{
		if (modifierKeyButtonsParent != null)
		{
			modifierKeyButtonsParent.SetActive(false);
		}
	}

	private void OnEscButtonClicked()
	{
		if (Client.Game != null)
		{
			Client.Game.EscOverride = true;
		}
	}

	private void OnEnlargeSmallButtonsChanged(int currentValue)
	{
		var enlarge = currentValue == (int) PreferenceEnums.EnlargeSmallButtons.On;
		if (UIManager.Gumps == null)
		{
			return;
		}
		foreach (var control in UIManager.Gumps)
		{
			ToggleSmallButtonsSize(control, enlarge);
		}
	}

	private void ToggleSmallButtonsSize(Control control, bool enlarge)
	{
		if (control is Button button)
		{
			button.ToggleSize(enlarge);
		}
		foreach (var child in control.Children)
		{
			ToggleSmallButtonsSize(child, enlarge);
		}
	}

	private void OnEnableAssistantChanged(int enableAssistantCurrentValue)
	{
#if ENABLE_INTERNAL_ASSISTANT
		if (UserPreferences.EnableAssistant.CurrentValue == (int) PreferenceEnums.EnableAssistant.On && Client.Game != null)
		{
			if (Plugin.LoadInternalAssistant())
			{
				//If we're already in the GameScene, trigger OnConnected callback since the Assistant won't receive it and
				//because it's needed for initialization
				if (Client.Game.Scene is GameScene)
				{
					Plugin.OnConnected();
				}
			}
		}
#endif
	}

	private void OnShowModifierKeyButtonsChanged(int currentValue)
	{
		if (Client.Game != null)
		{
			modifierKeyButtonsParent.SetActive(currentValue == (int) PreferenceEnums.ShowModifierKeyButtons.On);
		}
	}

	private void OnForceUseXbrChanged(int currentValue)
	{
		if (ProfileManager.Current != null)
		{
			ProfileManager.Current.UseXBR = currentValue == (int) PreferenceEnums.ForceUseXbr.On;
		}
	}

	private void OnContainerItemSelectionChanged(int currentValue)
	{
		ItemGump.PixelCheck = currentValue == (int) PreferenceEnums.ContainerItemSelection.Fine;
	}

	private void OnJoystickRunThresholdChanged(int currentValue)
	{
		if (Client.Game?.Scene is GameScene gameScene)
		{
			gameScene.JoystickRunThreshold = joystickRunThresholdValues[UserPreferences.JoystickRunThreshold.CurrentValue];
		}
	}

	private void OnJoystickDeadZoneChanged(int currentValue)
	{
		movementJoystick.deadZone = joystickDeadZoneValues[currentValue];
	}

	private static void OnTargetFrameRateChanged(int frameRate)
	{
		Application.targetFrameRate = frameRate;
	}
    
	private void UpdateTextureFiltering(int textureFiltering)
	{
		var filterMode = (FilterMode) textureFiltering;
		Texture2D.defaultFilterMode = filterMode;
		if (Client.Game != null)
		{
			var textures = FindObjectsOfType<Texture>();
			foreach (var t in textures)
			{
				t.filterMode = filterMode;
			}
			Client.Game.GraphicsDevice.Textures[1].UnityTexture.filterMode = FilterMode.Point;
			Client.Game.GraphicsDevice.Textures[2].UnityTexture.filterMode = FilterMode.Point;
		}
	}
	
	private void OnUseMouseOnMobileChanged(int useMouse)
	{
		UpdateMovementJoystick();
	}

	private void OnCustomScaleSizeChanged(int customScaleSize)
	{
		ApplyScalingFactor();
	}
	
	private void OnShowCloseButtonsChanged(int showCloseButtons)
	{
		Gump.CloseButtonsEnabled = showCloseButtons != 0;
        
		foreach (var control in UIManager.Gumps)
		{
			if (control is Gump gump)
			{
				gump.UpdateCloseButton();
			}
		}
	}

	private void Update()
	{
		if (Client.Game == null)
			return;

		if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
		{
			lastScreenWidth = Screen.width;
			lastScreenHeight = Screen.height;
			Client.Game.Window.ClientBounds = new Rectangle(0, 0, Screen.width, Screen.height);
			ApplyScalingFactor();
		}

		if (forceEnterWorld && Client.Game.Scene is LoginScene)
		{
			ProfileManager.Load("fakeserver", "fakeaccount", "fakecharacter");
			World.Mobiles.Add(World.Player = new PlayerMobile(0));
			World.MapIndex = 0;
			World.Player.X = 1443;
			World.Player.Y = 1677;
			World.Player.Z = 0;
			World.Player.UpdateScreenPosition();
			World.Player.AddToTile();
			Client.Game.SetScene(new GameScene());
		}

		float deltaTime = UnityEngine.Time.deltaTime;
		//Is this necessary? Wouldn't it slow down the game even further when it dips below 20 FPS?
        if(deltaTime > 0.050f)
        {
            deltaTime = 0.050f;
        }

        if (movementJoystick.isActiveAndEnabled && Client.Game.Scene is GameScene gameScene)
        {
	        gameScene.JoystickInput = new Microsoft.Xna.Framework.Vector2(movementJoystick.Input.x, -1 * movementJoystick.Input.y);
        }

        var keymod = SDL.SDL_Keymod.KMOD_NONE;
        if (ctrlKeyButtonPresenter.ToggledOn)
        {
	        keymod |= SDL.SDL_Keymod.KMOD_CTRL;
        }
        if (altKeyButtonPresenter.ToggledOn)
        {
	        keymod |= SDL.SDL_Keymod.KMOD_ALT;
        }
        if (shiftKeyButtonPresenter.ToggledOn)
        {
	        keymod |= SDL.SDL_Keymod.KMOD_SHIFT;
        }

        Client.Game.KeymodOverride = keymod;
        Client.Game.Tick(deltaTime);
	}

	private void OnPostRender()
    {
	    if (Client.Game == null)
		    return;

	    GL.LoadPixelMatrix( 0, Screen.width, Screen.height, 0 );
	    
        Client.Game.Batcher.UseGraphicsDrawTexture = useGraphicsDrawTexture;
        Client.Game.DrawUnity(UnityEngine.Time.deltaTime);

        forceEnterWorld = false;
    }

    public void StartGame(ServerConfiguration config)
    {
	    CUOEnviroment.ExecutablePath = config.GetPathToSaveFiles();

	    //Load and adjust settings
	    var settingsFilePath = Settings.GetSettingsFilepath();
	    if (File.Exists(settingsFilePath))
	    {
		    Settings.GlobalSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFilePath));
	    }
	    else
	    {
		    Settings.GlobalSettings = JsonConvert.DeserializeObject<Settings>(Resources.Load<TextAsset>("settings").text);
	    }

	    Settings.GlobalSettings.IP = config.UoServerUrl;
	    Settings.GlobalSettings.Port = ushort.Parse(config.UoServerPort);
	    
	    //Reset static encryption type variable
	    EncryptionHelper.Type = ENCRYPTION_TYPE.NONE;
	    Settings.GlobalSettings.Encryption = (byte) (config.UseEncryption ? 1 : 0);

	    //Empty the plugins array because no plugins are working at the moment
	    Settings.GlobalSettings.Plugins = new string[0];
	    
	    //If connecting to UO Outlands, set shard type to 2 for outlands
	    Settings.GlobalSettings.ShardType = config.UoServerUrl.ToLower().Contains("uooutlands") ? 2 : 0;

	    //Try to detect old client version to set ShardType to 1, for using StatusGumpOld. Otherwise, it's possible
	    //to get null-refs in StatusGumpModern.
	    if (ClientVersionHelper.IsClientVersionValid(config.ClientVersion, out var clientVersion))
	    {
		    if (clientVersion < ClientVersion.CV_308Z)
		    {
			    Settings.GlobalSettings.ShardType = 1;
		    }
	    }
	    
	    CUOEnviroment.IsOutlands = Settings.GlobalSettings.ShardType == 2;

	    Settings.GlobalSettings.ClientVersion = config.ClientVersion;
	    
	    if (Application.isMobilePlatform == false && string.IsNullOrEmpty(config.ClientPathForUnityEditor) == false)
	    {
		    Settings.GlobalSettings.UltimaOnlineDirectory = config.ClientPathForUnityEditor;
	    }
	    else
	    {
		    Settings.GlobalSettings.UltimaOnlineDirectory = config.GetPathToSaveFiles();
	    }

	    //This flag is tied to whether the GameCursor gets drawn, in a convoluted way
	    //On mobile platform, set this flag to true to prevent the GameCursor from being drawn
	    Settings.GlobalSettings.RunMouseInASeparateThread = Application.isMobilePlatform;

	    //Some mobile specific overrides need to be done on the Profile but they can only be done once the Profile has been loaded
	    ProfileManager.ProfileLoaded += OnProfileLoaded;

	    // Add an audio source and tell the media player to use it for playing sounds
	    Log.Start( LogTypes.All );

	    try
	    {
		    Client.SceneChanged += OnSceneChanged;
		    Client.Run();
#if ENABLE_INTERNAL_ASSISTANT
		    if (UserPreferences.EnableAssistant.CurrentValue == (int) PreferenceEnums.EnableAssistant.On)
		    {
			    Plugin.LoadInternalAssistant();
		    }
#endif
		    Client.Game.Exiting += OnGameExiting;
		    ApplyScalingFactor();

		    if (UserPreferences.ShowModifierKeyButtons.CurrentValue == (int) PreferenceEnums.ShowModifierKeyButtons.On)
		    {
			    modifierKeyButtonsParent.SetActive(true);
		    }
	    }
	    catch (Exception e)
	    {
		    Console.WriteLine(e);
		    OnError?.Invoke(e.ToString());
	    }
    }

    public static void Login()
    {
	    if (Client.Game == null || !(Client.Game.Scene is LoginScene loginScene) || loginScene.CurrentLoginStep != LoginSteps.Main)
	    {
		    return;
	    }
	    var loginGump = UIManager.Gumps.FirstOrDefault(g => g is LoginGump) as LoginGump;
	    loginGump?.OnButtonClick((int) LoginGump.Buttons.NextArrow);
    }

    private void OnProfileLoaded()
    {
	    //Disable auto move on mobile platform
	    ProfileManager.Current.DisableAutoMove = Application.isMobilePlatform;
	    //Prevent stack split gump from appearing on mobile
	    //ProfileManager.Current.HoldShiftToSplitStack = Application.isMobilePlatform;
	    //Scale items inside containers by default on mobile (won't have any effect if container scale isn't changed)
	    ProfileManager.Current.ScaleItemsInsideContainers = Application.isMobilePlatform;
	    OnForceUseXbrChanged(UserPreferences.ForceUseXbr.CurrentValue);
    }

    private void OnSceneChanged()
    {
	    ApplyScalingFactor();
	    UpdateMovementJoystick();
	    var isGameScene = Client.Game.Scene is GameScene;
	    SceneChanged?.Invoke(isGameScene);
	    if (isGameScene)
	    {
		    OnJoystickRunThresholdChanged(UserPreferences.JoystickRunThreshold.CurrentValue);
	    }
    }

    private void UpdateMovementJoystick()
    {
	    movementJoystick.gameObject.SetActive((Application.isMobilePlatform || showMovementJoystickOnNonMobilePlatforms)
	                                          && Client.Game != null && Client.Game.Scene is GameScene
	                                          && UserPreferences.UseMouseOnMobile.CurrentValue == 0);
    }

    private void ApplyScalingFactor()
    {
	    var scale = 1f;

	    if (Client.Game == null)
	    {
		    return;
	    }

	    var gameScene = Client.Game.Scene as GameScene;
	    var isGameScene = gameScene != null;

	    if (scaleGameToFitScreen)
	    {
		    var loginScale = Mathf.Min(Screen.width / 640f, Screen.height / 480f);
		    var gameScale = Mathf.Max(1, loginScale * 0.75f);
		    scale = isGameScene ? gameScale : loginScale;
	    }

	    if (UserPreferences.ScaleSize.CurrentValue != (int) PreferenceEnums.ScaleSizes.Default && isGameScene)
	    {
		    scale *= UserPreferences.ScaleSize.CurrentValue / 100f;
	    }

	    ((UnityGameWindow) Client.Game.Window).Scale = scale;
	    Client.Game.Batcher.scale = scale;
    }

    private void OnGameExiting(object sender, EventArgs e)
    {
	    Client.Game.UnloadContent();
	    Client.Game.Dispose();
	    OnExiting?.Invoke();
    }
}
