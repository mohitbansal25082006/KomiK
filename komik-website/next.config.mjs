/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  devIndicators: false,
  // A production check can build into its own folder (NEXT_DIST_DIR=.next-verify), so it never
  // overwrites the .next folder a running `npm run dev` is using.
  distDir: process.env.NEXT_DIST_DIR || ".next",
};

export default nextConfig;
