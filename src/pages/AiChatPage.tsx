import AiChat from "../components/AiChat";
import "../ai-chat.css"

AiChatPage.route = {
  path: '/ai-chat',
  menuLabel: 'AI Chat',
  index: 4
};

export default function AiChatPage() {
  return <>
    <AiChat />
  </>
}