import { useState } from "react";

export interface ViewShareLinkProps {
    title: string;
    link: string;
}

export default function ViewSharedLink(props: ViewShareLinkProps) {
    const [copied, setCopied] = useState(false);

    const handleCopy = async () => {
        const message = `Hej! Udostępniam Ci nagranie nagranie westa "${props.title}". Link będzie działał przez 7 dni. Aby wyświetlić nagranie odwiedź ten link: - ${props.link}`;
        await navigator.clipboard.writeText(message);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    const handleCopyOnlyLink = async () => {
        await navigator.clipboard.writeText(props.link);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    }

    return (
        <div className="content">
            <p>
                Link do nagrania: {props.title}
                <br />
                <a href={props.link} target="_blank" rel="noopener noreferrer">
                    {props.link}
                </a>
            </p>

            <div className="buttons">
                <button className="button is-primary" onClick={handleCopy}>
                    Kopiuj
                </button>
                <button className="button is-secondary" onClick={handleCopyOnlyLink}>
                    Kopiuj tylko link
                </button>
            </div>

            {copied && (
                <p className="has-text-success">Skopiowano do schowka</p>
            )}
        </div>
    );
}
