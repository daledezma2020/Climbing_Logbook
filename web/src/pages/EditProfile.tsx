import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { Upload } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Combobox } from "@/components/ui/combobox";
import ErrorToast from "@/components/log/ErrorToast";
import UserAvatar from "@/components/user/UserAvatar";
import { usePlaces } from "@/hooks/catalog-hooks";
import { useMyProfile, useUpdateMyProfile } from "@/hooks/user-hooks";
import { BIO_MAX_LENGTH, validateProfile } from "@/lib/profile-validation";
import type { UserProfile } from "@/types/user";

function Shell({
  subtitle,
  children,
}: {
  subtitle: string;
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="space-y-2">
          <h1 className="text-4xl font-bold tracking-tight text-slate-900">
            Edit profile
          </h1>
          <p className="text-slate-600">{subtitle}</p>
        </div>
        {children}
      </div>
    </div>
  );
}

export default function EditProfile() {
  const { loginWithRedirect, isAuthenticated, isLoading } = useAuth0();
  const { profile, loading: profileLoading, error, refetch } = useMyProfile();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      loginWithRedirect();
    }
  }, [isLoading, isAuthenticated, loginWithRedirect]);

  if (isLoading || !isAuthenticated) {
    return (
      <Shell subtitle="Taking you to sign in">
        <div className="rounded-lg border bg-white p-6 text-slate-600 shadow-sm">
          Loading...
        </div>
      </Shell>
    );
  }

  if (!profile) {
    return (
      <Shell subtitle="Loading your details">
        {error && !profileLoading ? (
          <ErrorToast title="Could not load your profile" message={error} />
        ) : (
          <div className="rounded-lg border bg-white p-6 text-slate-600 shadow-sm">
            Loading...
          </div>
        )}
      </Shell>
    );
  }

  return <ProfileForm profile={profile} onSaved={refetch} />;
}

function ProfileForm({
  profile,
  onSaved,
}: {
  profile: UserProfile;
  onSaved: () => void;
}) {
  const navigate = useNavigate();
  const { user } = useAuth0();
  const { places } = usePlaces();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [displayName, setDisplayName] = useState(profile.displayName);
  const [username, setUsername] = useState(profile.username);
  const [bio, setBio] = useState(profile.bio ?? "");
  const [pictureUrl, setPictureUrl] = useState(profile.pictureUrl ?? "");
  const [homePlaceId, setHomePlaceId] = useState(
    profile.homePlaceId ? String(profile.homePlaceId) : "",
  );

  const applySaved = useCallback(
    (saved: UserProfile) => {
      setPictureUrl(saved.pictureUrl ?? "");
      onSaved();
    },
    [onSaved],
  );

  const { saveProfile, uploadAvatar, saving, uploading, error, setError } =
    useUpdateMyProfile(applySaved);

  const placeOptions = places.map((place) => ({
    value: String(place.id),
    label: place.name,
  }));

  const handleSubmit = async () => {
    const payload = {
      displayName: displayName.trim(),
      username: username.trim(),
      bio: bio.trim() || null,
      pictureUrl: pictureUrl.trim() || null,
      homePlaceId: homePlaceId ? Number(homePlaceId) : null,
    };

    const validationError = validateProfile(payload);
    if (validationError) {
      setError(validationError);
      return;
    }

    const saved = await saveProfile(payload);
    if (saved) {
      navigate(`/users/${saved.username}`);
    }
  };

  const handleAvatarUpload = async (file: File) => {
    await uploadAvatar(file);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  return (
    <Shell subtitle="How other climbers see you">
      <div className="space-y-5 rounded-lg border bg-white p-6 shadow-sm">
        {error && (
          <ErrorToast
            title="Could not save profile"
            message={error}
            onDismiss={() => setError(null)}
          />
        )}

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">
            Display name
          </label>
          <Input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="How your name appears on your profile"
          />
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Username</label>
          <Input
            value={username}
            onChange={(e) => setUsername(e.target.value.toLowerCase())}
            placeholder="your-username"
          />
          <p className="text-xs text-slate-500">
            Your profile lives at /users/{username || "your-username"}
          </p>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Bio</label>
          <Textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            maxLength={BIO_MAX_LENGTH}
            placeholder="Tell other climbers about yourself"
          />
          <p className="text-right text-xs text-slate-500">
            {bio.length}/{BIO_MAX_LENGTH}
          </p>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Avatar</label>
          <div className="flex items-center gap-4">
            <UserAvatar
              displayName={displayName || username || "?"}
              pictureUrl={pictureUrl || null}
              fallbackPictureUrl={user?.picture}
              className="h-16 w-16"
            />
            <div className="min-w-0 flex-1 space-y-2">
              <Input
                value={pictureUrl}
                onChange={(e) => setPictureUrl(e.target.value)}
                placeholder="https://example.com/avatar.jpg"
              />
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) {
                    void handleAvatarUpload(file);
                  }
                }}
              />
              <Button
                type="button"
                variant="outline"
                className="gap-2"
                disabled={uploading}
                onClick={() => fileInputRef.current?.click()}
              >
                <Upload className="h-4 w-4" />
                {uploading ? "Uploading..." : "Upload an image"}
              </Button>
              <p className="text-xs text-slate-500">
                JPEG, PNG, or WebP, up to 2 MB. Uploading replaces the URL above.
              </p>
            </div>
          </div>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">
            Home place (optional)
          </label>
          <Combobox
            options={placeOptions}
            value={homePlaceId}
            onValueChange={setHomePlaceId}
            placeholder="Your home gym or crag"
            searchPlaceholder="Search places..."
            emptyMessage="No place found."
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button
            variant="outline"
            onClick={() => navigate(-1)}
            disabled={saving}
          >
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={saving}>
            {saving ? "Saving..." : "Save profile"}
          </Button>
        </div>
      </div>
    </Shell>
  );
}
