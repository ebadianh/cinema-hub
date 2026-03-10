import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import Cookies from "js-cookie";

type CookieConsentStatus = "unknown" | "accepted" | "rejected";

interface CookieConsentContextValue {
  status: CookieConsentStatus;
  accept: () => void;
  reject: () => void;
}

const CookieConsentContext = createContext<CookieConsentContextValue | undefined>(undefined);

const COOKIE_NAME = "cookie_consent";
const COOKIE_ACCEPTED = "accepted";
const COOKIE_REJECTED = "rejected";

interface CookieConsentProviderProps {
  children: ReactNode;
}

export function CookieConsentProvider({ children }: CookieConsentProviderProps) {
  const [status, setStatus] = useState<CookieConsentStatus>("unknown");

  useEffect(() => {
    const value = Cookies.get(COOKIE_NAME);
    if (value === COOKIE_ACCEPTED || value === COOKIE_REJECTED) {
      setStatus(value);
    }
  }, []);

  const accept = () => {
    setStatus("accepted");
    Cookies.set(COOKIE_NAME, COOKIE_ACCEPTED, {
      expires: 365,
      path: "/",
      sameSite: "Lax",
    });
  };

  const reject = () => {
    setStatus("rejected");
    Cookies.set(COOKIE_NAME, COOKIE_REJECTED, {
      expires: 365,
      path: "/",
      sameSite: "Lax",
    });
  };

  return (
    <CookieConsentContext.Provider value={{ status, accept, reject }}>
      {children}
    </CookieConsentContext.Provider>
  );
}

export function useCookieConsent(): CookieConsentContextValue {
  const context = useContext(CookieConsentContext);
  if (!context) {
    throw new Error("useCookieConsent must be used within a CookieConsentProvider");
  }
  return context;
}

