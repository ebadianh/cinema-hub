import AiChatWidget from "./AiChatWidget";

AiChatPage.route = {
  path: "/ai-chat",
  menuLabel: "AI Chat",
  index: 4,
};

export default function AiChatPage() {
  return <AiChatWidget />;
}
