import AiChat from "../components/AiChat";

AiChatPage.route = {
  path: '/ai-chat',
  menuLabel: 'AI Chat',
  index: 4
};

export default function AiChatPage() {
  return <>
    <p>Perhaps it would be better if the AI chat was always available on all pages rather than just on a specific page?</p>
    <AiChat />
  </>
}