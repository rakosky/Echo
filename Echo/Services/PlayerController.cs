using Echo.Models.Settings;
using Echo.Services.ImageAnalysis;
using Echo.Util;
using Microsoft.Extensions.Logging;
using System.Drawing;
using static Echo.Extern.User32;

namespace Echo.Services
{
    public class PlayerController
    {
        readonly ILogger<PlayerController> _logger;
        readonly HotkeySettings _hotkeys;
        readonly InputSender _inputSender;
        readonly ScreenshotProvider _screenshotProvider;
        readonly GameAnalyzer _gameAnalyzer;
        readonly MapAnalyzer _mapAnalyzer;

        private int _gainBreathDelay = 10000;

        public PlayerController(
            ILogger<PlayerController> logger,
            AppSettings settings,
            InputSender inputSender,
            ScreenshotProvider screenshotProvider,
            GameAnalyzer gameAnalyzer,
            MapAnalyzer mapAnalyzer)
        {
            _logger = logger;
            _hotkeys = settings.Hotkeys;
            _inputSender = inputSender;
            _screenshotProvider = screenshotProvider;
            _gameAnalyzer = gameAnalyzer;
            _mapAnalyzer = mapAnalyzer;
        }

        public async Task TeleToMacroMap(Bitmap mapImg, CancellationToken ct)
        {
            _inputSender.ReleaseAllPressed();
            await Task.Delay(1000, ct);
            _inputSender.SendKey(_hotkeys.MapKey);
            await Task.Delay(2000, ct);

            var imageData = await _screenshotProvider.GetLatestPixels();

            Point? mapCoords = ImageFunctions.FindSubImageCoords(imageData, mapImg, .1)
                .LocationOrDefault();

            if (mapCoords != null)
            {
                await Task.Delay(2000, ct);
                _inputSender.ClickOnPoint(new Point { X = 100, Y = 100 });
                await Task.Delay(1000, ct);
                _inputSender.ClickOnPoint(mapCoords.Value);
                await Task.Delay(20, ct);
                _inputSender.ClickOnPoint(mapCoords.Value);
                await Task.Delay(1000, ct);
                _inputSender.SendKey(ScanCodeShort.RETURN);
                await Task.Delay(100, ct);
                _inputSender.SendKey(ScanCodeShort.RETURN);
                await Task.Delay(100, ct);
                _inputSender.SendKey(ScanCodeShort.RETURN);
            }
            else
            {
                _logger.LogInformation("Unable to find desired map");
            }
        }
        public async Task<bool> CC(bool goUpCh, bool preWaitForBreath, bool attackIfFail, CancellationToken ct)
        {
            _logger.LogInformation($"CC attempt @ {DateTime.Now} with increase ch = {goUpCh}, preWaitForBreath = {preWaitForBreath}, attackIfFail = {attackIfFail}.");
            _inputSender.ReleaseAllPressed();
            await Task.Delay(500, ct);
            int keystrokeDelay = 150;
            _inputSender.SendKey(ScanCodeShort.RIGHT);//tap right incase chat box is open(idk why this happens)
            if (preWaitForBreath)
                await Task.Delay(_gainBreathDelay, ct);

            _inputSender.SendKey(_hotkeys.MenuKey);
            await Task.Delay(keystrokeDelay, ct);
            if (goUpCh) _inputSender.SendKey(ScanCodeShort.RIGHT);
            else _inputSender.SendKey(ScanCodeShort.LEFT);
            await Task.Delay(keystrokeDelay, ct);
            _inputSender.SendKey(ScanCodeShort.RETURN);
            await Task.Delay(keystrokeDelay, ct);

            int checkForDuration = 800;
            int checkBetweenDelay = 200;
            for (int i = 0; i < checkForDuration; i += checkBetweenDelay)
            {                
                if (await _gameAnalyzer.CheckForMapOrChannelChange())
                {
                    return true;
                }
                
                await Task.Delay(checkBetweenDelay, ct);
            }

            if (attackIfFail)
            {
                for (int i = 0; i < 10; i++)
                {
                    _inputSender.SendKey(_hotkeys.AttackKey);
                    await Task.Delay(500, ct);
                }
                await Task.Delay(_gainBreathDelay, ct);
            }
            return false;

        }
        //attempt to move player to (x, y) position on screen, relative to minimap
        public async Task GoTo(Point target, CancellationToken ct)
        {
            var recheckLoopDelay = 100;
            _logger.LogInformation($"GoTo X = {target.X} and Y = {target.Y}.");

            var posQueue = new Queue<Point?>();
            while (!ct.IsCancellationRequested)
            {
                var pixelSnapshot = await _screenshotProvider.GetLatestPixels();
                Point? playerLoc = _mapAnalyzer.GetPlayerLocation(pixelSnapshot);

                if (playerLoc is null)
                {
                    _logger.LogError("Cant find player");
                    continue;
                }

                int x1 = playerLoc.Value.X;
                int y1 = playerLoc.Value.Y;

                int x2 = target.X;
                int y2 = target.Y;


                //There are delays between taking a screenshot, processing the image, sending the key press, and game server ping.
                // X distance check & handling
                if (Math.Abs(x1 - x2) > 5)
                {
                    _inputSender.SendKey(ScanCodeShort.DOWN, KeyPressType.UP);

                    if (x1 < x2)
                    {
                        _inputSender.SendKey(ScanCodeShort.LEFT, KeyPressType.UP);
                        _inputSender.SendKey(ScanCodeShort.RIGHT, KeyPressType.DOWN);
                    }
                    // Player is to the right of target x-position.
                    else
                    {
                        _inputSender.SendKey(ScanCodeShort.RIGHT, KeyPressType.UP);
                        _inputSender.SendKey(ScanCodeShort.LEFT, KeyPressType.DOWN);
                    }
                    if (Math.Abs(x2 - x1) > 30)
                    {
                        _inputSender.SendKey(_hotkeys.JumpKey);
                        _inputSender.SendKey(_hotkeys.JumpKey);
                    }
                }

                // Player has reached target x-destination, move to point one Y axis
                else
                {
                    _inputSender.ReleaseAllPressed();

                    if (Math.Abs(y2 - y1) < 10)
                    {
                        // Player has reached target y-destination, release all held keys.
                        _inputSender.ReleaseAllPressed();
                        break;
                    }
                    // Player is above target y-position.
                    else if (y1 < y2)
                    {
                        _inputSender.SendKey(ScanCodeShort.DOWN, KeyPressType.DOWN);
                        _inputSender.SendKey(_hotkeys.JumpKey);
                    }
                    // Player is below target y-position.
                    else
                    {
                        if (y1 - y2 > 30)
                        {
                            //up jump into rope lift
                            _inputSender.ReleaseAllPressed();

                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.DOWN);
                            await Task.Delay(78, ct);
                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.UP);
                            await Task.Delay(38, ct);
                            _inputSender.SendKey(ScanCodeShort.UP, KeyPressType.DOWN);
                            await Task.Delay(61, ct);
                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.DOWN);
                            await Task.Delay(136, ct);
                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.UP);
                            await Task.Delay(27, ct);
                            _inputSender.SendKey(ScanCodeShort.UP, KeyPressType.UP);

                            _inputSender.SendKey(_hotkeys.RopeLiftKey);
                        }
                        else if (y1 - y2 > 5)
                        {
                            //up jump
                            _inputSender.ReleaseAllPressed();
                            await Task.Delay(78, ct);

                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.DOWN);
                            await Task.Delay(78, ct);
                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.UP);
                            await Task.Delay(38, ct);
                            _inputSender.SendKey(ScanCodeShort.UP, KeyPressType.DOWN);
                            await Task.Delay(61, ct);
                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.DOWN);
                            await Task.Delay(136, ct);
                            _inputSender.SendKey(_hotkeys.JumpKey, KeyPressType.UP);
                            await Task.Delay(27, ct);
                            _inputSender.SendKey(ScanCodeShort.UP, KeyPressType.UP);
                        }
                        else
                        {
                            // simple jump
                            _inputSender.SendKey(ScanCodeShort.UP);
                            _inputSender.SendKey(_hotkeys.JumpKey);
                        }
                    }
                    // Delay for player falling down or jumping up.
                    await Task.Delay(500, ct);
                }

                // If player is at the same position for too long we try to unstick them
                posQueue.Enqueue(playerLoc);
                if (posQueue.Count > 4)
                {
                    if (posQueue.All(p => p.Value.X == playerLoc.Value.X && p.Value.Y == playerLoc.Value.Y))
                    {
                        _inputSender.SendKey(ScanCodeShort.RIGHT, KeyPressType.DOWN);
                        await Task.Delay(30, ct);
                        _inputSender.SendKey(_hotkeys.JumpKey);
                        await Task.Delay(30, ct);
                        _inputSender.SendKey(ScanCodeShort.RIGHT, KeyPressType.UP);
                    }
                    posQueue.Clear();
                }

                await Task.Delay(recheckLoopDelay, ct);
            }
            _logger.LogInformation($"GoTo X = {target.X} and Y = {target.Y} complete.");
            _inputSender.ReleaseAllPressed();
        }

        public async Task SendMessage(string message, CancellationToken ct)
        {
            _inputSender.SendKey(ScanCodeShort.RETURN);

            await Task.Delay(500, ct);

            foreach (var c in message.ToLower())
            {
                ScanCodeShort? scanCode = CharToScanCode(c);
                if (scanCode.HasValue)
                {
                    _inputSender.SendKey(scanCode.Value);
                    Thread.Sleep(10);
                }
            }

            await Task.Delay(500, ct);

            _inputSender.SendKey(ScanCodeShort.RETURN);
            _inputSender.SendKey(ScanCodeShort.RETURN);
        }

        private ScanCodeShort? CharToScanCode(char c)
        {
            switch (c)
            {
                case 'a': return ScanCodeShort.KEY_A;
                case 'b': return ScanCodeShort.KEY_B;
                case 'c': return ScanCodeShort.KEY_C;
                case 'd': return ScanCodeShort.KEY_D;
                case 'e': return ScanCodeShort.KEY_E;
                case 'f': return ScanCodeShort.KEY_F;
                case 'g': return ScanCodeShort.KEY_G;
                case 'h': return ScanCodeShort.KEY_H;
                case 'i': return ScanCodeShort.KEY_I;
                case 'j': return ScanCodeShort.KEY_J;
                case 'k': return ScanCodeShort.KEY_K;
                case 'l': return ScanCodeShort.KEY_L;
                case 'm': return ScanCodeShort.KEY_M;
                case 'n': return ScanCodeShort.KEY_N;
                case 'o': return ScanCodeShort.KEY_O;
                case 'p': return ScanCodeShort.KEY_P;
                case 'q': return ScanCodeShort.KEY_Q;
                case 'r': return ScanCodeShort.KEY_R;
                case 's': return ScanCodeShort.KEY_S;
                case 't': return ScanCodeShort.KEY_T;
                case 'u': return ScanCodeShort.KEY_U;
                case 'v': return ScanCodeShort.KEY_V;
                case 'w': return ScanCodeShort.KEY_W;
                case 'x': return ScanCodeShort.KEY_X;
                case 'y': return ScanCodeShort.KEY_Y;
                case 'z': return ScanCodeShort.KEY_Z;
                case ' ': return ScanCodeShort.SPACE;
            }
            return null;
        }
    }
}
