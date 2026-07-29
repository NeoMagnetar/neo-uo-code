# Neo UO Smooth Follow - Razor Enhanced client movement bridge

NEAR_DISTANCE = 2
COMFORT_DISTANCE = 4
FAR_DISTANCE = 10
VERY_FAR_DISTANCE = 16

RUN_DELAY_MS = 180
SOFT_DELAY_MS = 240
IDLE_DELAY_MS = 220
PATHFIND_DELAY_MS = 900
MAIN_LOOP_IDLE_MS = 250
STUCK_THRESHOLD = 5

START_PREFIX = "AIGM_SMOOTHFOLLOW_START serial="
STOP_PREFIX = "AIGM_SMOOTHFOLLOW_STOP"

target_serial = 0
last_action = "idle"
stuck_ticks = 0
last_journal = -1.0


def direction_to_target(px, py, tx, ty):
    dx = tx - px
    dy = ty - py

    if dx == 0 and dy < 0:
        return "North"
    if dx == 0 and dy > 0:
        return "South"
    if dx > 0 and dy == 0:
        return "East"
    if dx < 0 and dy == 0:
        return "West"
    if dx > 0 and dy < 0:
        return "Up"
    if dx < 0 and dy < 0:
        return "Left"
    if dx > 0 and dy > 0:
        return "Right"
    if dx < 0 and dy > 0:
        return "Down"

    return None


def parse_start_serial(text):
    start = text.find(START_PREFIX)
    if start < 0:
        return 0

    raw = text[start + len(START_PREFIX):].split(" ")[0].strip()
    try:
        if raw.lower().startswith("0x"):
            return int(raw, 16)
        return int(raw)
    except:
        return 0


def read_directives():
    global target_serial, last_action, stuck_ticks, last_journal

    entries = Journal.GetJournalEntry(last_journal)
    for entry in entries:
        if entry.Timestamp > last_journal:
            last_journal = entry.Timestamp

        text = entry.Text
        if text is None:
            continue

        if STOP_PREFIX in text:
            if target_serial != 0:
                Misc.SendMessage("NeoUO Smooth Follow stopped.", 68)
            target_serial = 0
            last_action = "idle"
            stuck_ticks = 0
            continue

        serial = parse_start_serial(text)
        if serial != 0:
            target_serial = serial
            last_action = "start"
            stuck_ticks = 0
            Misc.SendMessage("NeoUO Smooth Follow target serial=" + hex(target_serial), 68)


def follow_target():
    global last_action, stuck_ticks

    target = Mobiles.FindBySerial(target_serial)
    if target is None:
        if last_action != "missing":
            Misc.SendMessage("SmoothFollow: target not found " + hex(target_serial), 33)
        last_action = "missing"
        Misc.Pause(750)
        return

    dist = Player.DistanceTo(target)
    if dist <= NEAR_DISTANCE:
        stuck_ticks = 0
        last_action = "idle"
        Misc.Pause(IDLE_DELAY_MS)
        return

    tx = target.Position.X
    ty = target.Position.Y
    tz = target.Position.Z

    if dist >= VERY_FAR_DISTANCE:
        if last_action != "pathfind_far":
            Misc.SendMessage("SmoothFollow: catch-up pathfind.", 53)
        Player.PathFindTo(tx, ty, tz)
        last_action = "pathfind_far"
        stuck_ticks = 0
        Misc.Pause(PATHFIND_DELAY_MS)
        return

    if dist >= FAR_DISTANCE:
        if last_action != "pathfind":
            Misc.SendMessage("SmoothFollow: pathfind.", 53)
        Player.PathFindTo(tx, ty, tz)
        last_action = "pathfind"
        stuck_ticks = 0
        Misc.Pause(PATHFIND_DELAY_MS)
        return

    px = Player.Position.X
    py = Player.Position.Y
    direction = direction_to_target(px, py, tx, ty)
    if direction is None:
        Misc.Pause(MAIN_LOOP_IDLE_MS)
        return

    before = dist
    Player.Run(direction)
    Misc.Pause(80)
    after = Player.DistanceTo(target)

    if after < before:
        stuck_ticks = 0
    else:
        stuck_ticks += 1

    if stuck_ticks >= STUCK_THRESHOLD:
        Misc.SendMessage("SmoothFollow: stalled, trying pathfind.", 53)
        Player.PathFindTo(tx, ty, tz)
        stuck_ticks = 0
        last_action = "recover_pathfind"
        Misc.Pause(PATHFIND_DELAY_MS)
        return

    last_action = "run"
    if dist <= COMFORT_DISTANCE:
        Misc.Pause(SOFT_DELAY_MS)
    else:
        Misc.Pause(RUN_DELAY_MS)


Misc.SendMessage("NeoUO Smooth Follow listener ready.", 68)

while True:
    try:
        read_directives()
        if target_serial != 0:
            follow_target()
        else:
            Misc.Pause(MAIN_LOOP_IDLE_MS)
    except Exception as ex:
        Misc.SendMessage("SmoothFollow error: " + str(ex), 33)
        Misc.Pause(1000)
