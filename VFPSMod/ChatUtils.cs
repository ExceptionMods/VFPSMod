namespace VFPSMod
{
    class ChatUtils
    {
        public static void SendLocalChatMessage(string localPlayerName, string text, Talker.Type type)
        {
            if (Player.m_localPlayer != null)
            {
                Chat.instance.m_hideTimer = -3f;
                Chat.instance.m_chatWindow.gameObject.SetActive(true);
                Chat.instance.AddString(localPlayerName, text, type);
            }
        }
    }
}
