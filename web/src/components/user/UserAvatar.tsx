import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";

interface UserAvatarProps {
  displayName: string;
  pictureUrl?: string | null;
  fallbackPictureUrl?: string | null;
  className?: string;
}

function initialsOf(displayName: string) {
  return displayName
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

export default function UserAvatar({
  displayName,
  pictureUrl,
  fallbackPictureUrl,
  className,
}: UserAvatarProps) {
  const src = pictureUrl ?? fallbackPictureUrl ?? undefined;

  return (
    <Avatar className={className}>
      <AvatarImage src={src} alt={displayName} />
      <AvatarFallback>{initialsOf(displayName)}</AvatarFallback>
    </Avatar>
  );
}
