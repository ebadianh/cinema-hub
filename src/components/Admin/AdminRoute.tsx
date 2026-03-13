import { useEffect, useState, type JSX } from "react";
import { Navigate } from "react-router-dom";

type Props = {
  children: JSX.Element;
};

/* Denna metod fungerar som en guardrail för att inte tillåta användare att komma åt admin routes 

Obehöriga användare kommer att routas om till homepage när de försöker komma åt dessa skyddade vyerna

Det finns spärrar i vår backend i form av ACL, men denna existerar också som ett skydd på frontend för att inte tillåta
att man kan routa hit överhuvudtaget.

*/

export default function AdminRoute({ children }: Props) {
  const [status, setStatus] = useState<"loading" | "allowed" | "denied">(
    "loading",
  );

  useEffect(() => {
    fetch("/api/login", {
      credentials: "include",
    })
      .then((res) => res.json())
      .then((data) => {
        if (!data.error && data.role === "admin") {
          setStatus("allowed");
        } else {
          setStatus("denied");
        }
      })
      .catch(() => setStatus("denied"));
  }, []);

  if (status === "loading") {
    return <div>Laddar...</div>;
  }

  if (status === "denied") {
    return <Navigate to="/" replace />;
  }

  return children;
}
