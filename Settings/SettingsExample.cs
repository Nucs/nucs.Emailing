/*using nucs.Collections;
using nucs.SystemCore.Settings;

public class Settings : AppSettings<Settings> {
    public ImprovedList<KeyBindingDetails> kbDB;
    public long Runs = -1;
#if DEBUG
        public bool App_AutoStartup = false;
#else
    public bool App_AutoStartup = true;
#endif
    public bool CSharp_AddNamespaces = true;

    public override void Save() {
        base.Save();
        if (Program.MainForm != null)
            Program.MainForm.initKeyBindings();
    }
}*/

/*using nucs.Collections;
using nucs.SystemCore.Settings;

public class Settings : EncryptedAppSettings<Settings> {
    public ImprovedList<KeyBindingDetails> kbDB;
    public long Runs = -1;
#if DEBUG
        public bool App_AutoStartup = false;
#else
    public bool App_AutoStartup = true;
#endif
    public bool CSharp_AddNamespaces = true;


    public override string GenerateSeed() {
        return "cHJvcGhlY3lwYXNzd29yZA==".DecodeBase64(); //prophecypassword
    }

    public override void Save() {
        base.Save();
        if (Program.MainForm != null)
            Program.MainForm.initKeyBindings();
    }
}*/